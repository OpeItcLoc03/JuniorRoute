﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

using Junior.Common;
using Junior.Route.AutoRouting;
using Junior.Route.AutoRouting.AuthenticationStrategies;
using Junior.Route.AutoRouting.ClassFilters;
using Junior.Route.AutoRouting.Containers;
using Junior.Route.AutoRouting.IdMappers;
using Junior.Route.AutoRouting.MethodFilters;
using Junior.Route.AutoRouting.NameMappers;
using Junior.Route.AutoRouting.ResolvedRelativeUrlMappers;
using Junior.Route.AutoRouting.ResolvedRelativeUrlMappers.Attributes;
using Junior.Route.AutoRouting.ResponseMappers;
using Junior.Route.AutoRouting.RestrictionMappers;
using Junior.Route.AutoRouting.RestrictionMappers.Attributes;
using Junior.Route.Routing;
using Junior.Route.Routing.AuthenticationProviders;
using Junior.Route.Routing.Restrictions;

using NUnit.Framework;

using Rhino.Mocks;

namespace Junior.Route.UnitTests.AutoRouting
{
	public static class AutoRouteCollectionTester
	{
		[TestFixture]
		public class When_allowing_duplicate_route_names_and_attempting_to_add_duplicate_route_names
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper
					.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask())
					.Return(null);
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_autoRouteCollection = new AutoRouteCollection(true)
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper);
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;

			public class Endpoint
			{
				public void Method1()
				{
				}

				public void Method2()
				{
				}
			}

			[Test]
#warning Update to use async Assert.That(..., Throws.Nothing) when NUnit 2.6.3 becomes available
			public async void Must_not_throw_exception()
			{
				await _autoRouteCollection.GenerateRouteCollectionAsync();
			}
		}

		[TestFixture]
		public class When_assigning_authentication_provider
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_authenticationProvider = MockRepository.GenerateMock<IAuthenticationProvider>();
				_authenticationProvider
					.Stub(arg => arg.AuthenticateAsync(Arg<HttpRequestBase>.Is.Anything, Arg<HttpResponseBase>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(AuthenticationResult.AuthenticationSucceeded.AsCompletedTask());
				_authenticationStrategy = MockRepository.GenerateMock<IAuthenticationStrategy>();
				_authenticationStrategy.Stub(arg => arg.MustAuthenticateAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(true.AsCompletedTask());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.Authenticate(_authenticationProvider, _authenticationStrategy);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IAuthenticationProvider _authenticationProvider;
			private IAuthenticationStrategy _authenticationStrategy;
			private Route.Routing.Route[] _routes;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public async void Must_use_strategy_to_assign_provider()
			{
				var request = MockRepository.GenerateMock<HttpRequestBase>();
				var response = MockRepository.GenerateMock<HttpResponseBase>();

				await _routes[0].AuthenticateAsync(request, response);

				_authenticationStrategy.AssertWasCalled(arg => arg.MustAuthenticateAsync(typeof(Endpoint), typeof(Endpoint).GetMethod("Method")));
				_authenticationProvider.AssertWasCalled(arg => arg.AuthenticateAsync(request, response, _routes[0]));
			}
		}

		[TestFixture]
		public class When_attempting_to_add_duplicate_route_ids
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper
					.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = (NameResult.NameMapped(Guid.NewGuid().ToString())).AsCompletedTask())
					.Return(null);
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper);
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;

			public class Endpoint
			{
				public void Method1()
				{
				}

				public void Method2()
				{
				}
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
#warning Update to use async Assert.That(..., Throws.InstanceOf<>) when NUnit 2.6.3 becomes available
			public async void Must_throw_exception()
			{
				await _autoRouteCollection.GenerateRouteCollectionAsync();
			}
		}

		[TestFixture]
		public class When_disallowing_duplicate_route_names_and_attempting_to_add_duplicate_route_names
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper
					.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = (IdResult.IdMapped(Guid.NewGuid())).AsCompletedTask())
					.Return(null);
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper);
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;

			public class Endpoint
			{
				public void Method1()
				{
				}

				public void Method2()
				{
				}
			}

			[Test]
			[ExpectedException(typeof(ArgumentException))]
#warning Update to use async Assert.That(..., Throws.Nothing) when NUnit 2.6.3 becomes available
			public async void Must_throw_exception()
			{
				await _autoRouteCollection.GenerateRouteCollectionAsync();
			}
		}

		[TestFixture]
		public class When_filtering_classes
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(IncludedEndpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			public class IncludedEndpoint
			{
				public void Method()
				{
				}
			}

			public class ExcludedEndpoint
			{
				public void Method()
				{
				}
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private Route.Routing.Route[] _routes;
			private IResponseMapper _responseMapper;

			[Test]
			public void Must_consider_correct_classes()
			{
				Assert.That(_routes, Has.Length.EqualTo(1));
				_responseMapper.AssertWasCalled(
					arg => arg.MapAsync(
						Arg<Func<IContainer>>.Is.Anything,
						Arg<Type>.Is.Equal(typeof(IncludedEndpoint)),
						Arg<MethodInfo>.Is.Equal(typeof(IncludedEndpoint).GetMethod("Method")),
						Arg<Route.Routing.Route>.Is.Anything));
				_responseMapper.AssertWasNotCalled(
					arg => arg.MapAsync(
						Arg<Func<IContainer>>.Is.Anything,
						Arg<Type>.Is.Equal(typeof(ExcludedEndpoint)),
						Arg<MethodInfo>.Is.Anything,
						Arg<Route.Routing.Route>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_filtering_methods
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_methodFilter = MockRepository.GenerateMock<IMethodFilter>();
				_methodFilter
					.Stub(arg => arg.MatchesAsync(Arg<MethodInfo>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = (((MethodInfo)arg.Arguments.First()).Name == "IncludedMethod").AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.MethodFilters(_methodFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			public class Endpoint
			{
				public void IncludedMethod()
				{
				}

				public void ExcludedMethod()
				{
				}
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private Route.Routing.Route[] _routes;
			private IResponseMapper _responseMapper;
			private IMethodFilter _methodFilter;

			[Test]
			public void Must_consider_correct_methods()
			{
				Assert.That(_routes, Has.Length.EqualTo(1));
				_responseMapper.AssertWasCalled(
					arg => arg.MapAsync(
						Arg<Func<IContainer>>.Is.Anything,
						Arg<Type>.Is.Equal(typeof(Endpoint)),
						Arg<MethodInfo>.Is.Equal(typeof(Endpoint).GetMethod("IncludedMethod")),
						Arg<Route.Routing.Route>.Is.Anything));
				_responseMapper.AssertWasNotCalled(
					arg => arg.MapAsync(
						Arg<Func<IContainer>>.Is.Anything,
						Arg<Type>.Is.Anything,
						Arg<MethodInfo>.Is.Equal("ExcludedMethod"),
						Arg<Route.Routing.Route>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_generating_routes_with_ignored_resolved_relative_url_mapper_types
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped(Guid.NewGuid().ToString()).AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_httpRuntime = MockRepository.GenerateMock<IHttpRuntime>();
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(typeof(Endpoint).Assembly)
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlFromRelativeClassNamespaceAndClassName("")
					.ResolvedRelativeUrlUsingAttribute()
					.ResponseMapper(_responseMapper)
					.RestrictionContainer(new DefaultRestrictionContainer(_httpRuntime));
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResponseMapper _responseMapper;
			private Route.Routing.Route[] _routes;
			private IHttpRuntime _httpRuntime;

			public class Endpoint
			{
				[ResolvedRelativeUrl("")]
				[IgnoreResolvedRelativeUrlMapperType(typeof(ResolvedRelativeUrlFromRelativeClassNamespaceAndClassNameMapper))]
				public void Method()
				{
				}
			}

			[Test]
			public void Must_ignore_resolved_relative_url_mapper_types_specified_in_ignoreresolvedrelativeurlmappertypeattribute()
			{
				Assert.That(_routes[0].ResolvedRelativeUrl, Is.Empty);
			}
		}

		[TestFixture]
		public class When_generating_routes_with_ignored_restriction_mapper_types
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped(Guid.NewGuid().ToString()).AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_httpRuntime = MockRepository.GenerateMock<IHttpRuntime>();
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(typeof(Endpoint).Assembly)
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.RestrictUsingAttributes<UrlRelativePathAttribute>()
					.RestrictRelativePathsToRelativeClassNamespaceAndClassName("")
					.ResponseMapper(_responseMapper)
					.RestrictionContainer(new DefaultRestrictionContainer(_httpRuntime));
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IResponseMapper _responseMapper;
			private Route.Routing.Route[] _routes;
			private IHttpRuntime _httpRuntime;

			public class Endpoint
			{
				[UrlRelativePath("")]
				[IgnoreRestrictionMapperType(typeof(UrlRelativePathFromRelativeClassNamespaceAndClassNameMapper))]
				public void Method()
				{
				}
			}

			[Test]
			public async void Must_ignore_restriction_mapper_types_specified_in_ignorerestrictionmappertypeattribute()
			{
				_httpRuntime.Stub(arg => arg.AppDomainAppVirtualPath).Return("/");

				var request = MockRepository.GenerateMock<HttpRequestBase>();

				request.Stub(arg => arg.Url).Return(new Uri("http://localhost"));

				MatchResult matchResult = await _routes[0].MatchesRequestAsync(request);

				Assert.That(matchResult.ResultType, Is.EqualTo(MatchResultType.RouteMatched));
			}
		}

		[TestFixture]
		public class When_mapping_ids
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper1 = MockRepository.GenerateMock<IIdMapper>();
				_idMapper1.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.Parse("1dffe3ee-1ade-4aa2-835a-9cb91b7e31c4")).AsCompletedTask());
				_idMapper2 = MockRepository.GenerateMock<IIdMapper>();
				_idMapper2.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.Parse("493e725c-cbc1-4ea4-b6d1-350018d4542d")).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper1)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper1;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IResponseMapper _responseMapper;
			private Route.Routing.Route[] _routes;
			private IIdMapper _idMapper2;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public void Must_assign_mapped_id()
			{
				Assert.That(_routes[0].Id, Is.EqualTo(Guid.Parse("1dffe3ee-1ade-4aa2-835a-9cb91b7e31c4")));
			}

			[Test]
			public void Must_map_using_first_matching_mapper()
			{
				_idMapper1.AssertWasCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
				_idMapper2.AssertWasNotCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_mapping_names
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper1 = MockRepository.GenerateMock<INameMapper>();
				_nameMapper1.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name1").AsCompletedTask());
				_nameMapper2 = MockRepository.GenerateMock<INameMapper>();
				_nameMapper2.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name2").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper1, _nameMapper2)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper1;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IResponseMapper _responseMapper;
			private INameMapper _nameMapper2;
			private Route.Routing.Route[] _routes;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public void Must_assign_mapped_name()
			{
				Assert.That(_routes[0].Name, Is.EqualTo("name1"));
			}

			[Test]
			public void Must_map_using_first_matching_mapper()
			{
				_nameMapper1.AssertWasCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
				_nameMapper2.AssertWasNotCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_mapping_resolved_relative_urls
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper1 = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper1.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative1").AsCompletedTask());
				_resolvedRelativeUrlMapper2 = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper2.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative2").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper1)
					.ResponseMapper(_responseMapper);
				_routes = _autoRouteCollection.GenerateRouteCollectionAsync().Result.ToArray();
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper1;
			private IResponseMapper _responseMapper;
			private Route.Routing.Route[] _routes;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper2;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public void Must_assign_mapped_resolved_relative_url()
			{
				Assert.That(_routes[0].ResolvedRelativeUrl, Is.EqualTo("relative1"));
			}

			[Test]
			public void Must_map_using_first_matching_mapper()
			{
				_resolvedRelativeUrlMapper1.AssertWasCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
				_resolvedRelativeUrlMapper2.AssertWasNotCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_mapping_responses
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper);
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IResponseMapper _responseMapper;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public async void Must_map_using_mapper()
			{
				await _autoRouteCollection.GenerateRouteCollectionAsync();

				_responseMapper.AssertWasCalled(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything));
			}
		}

		[TestFixture]
		public class When_mapping_restrictions
		{
			[SetUp]
			public void SetUp()
			{
				_classFilter = MockRepository.GenerateMock<IClassFilter>();
				_classFilter
					.Stub(arg => arg.MatchesAsync(Arg<Type>.Is.Anything))
					.WhenCalled(arg => arg.ReturnValue = ((Type)arg.Arguments.First() == typeof(Endpoint)).AsCompletedTask())
					.Return(false.AsCompletedTask());
				_idMapper = MockRepository.GenerateMock<IIdMapper>();
				_idMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(IdResult.IdMapped(Guid.NewGuid()).AsCompletedTask());
				_nameMapper = MockRepository.GenerateMock<INameMapper>();
				_nameMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(NameResult.NameMapped("name").AsCompletedTask());
				_resolvedRelativeUrlMapper = MockRepository.GenerateMock<IResolvedRelativeUrlMapper>();
				_resolvedRelativeUrlMapper.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything)).Return(ResolvedRelativeUrlResult.ResolvedRelativeUrlMapped("relative").AsCompletedTask());
				_responseMapper = MockRepository.GenerateMock<IResponseMapper>();
				_responseMapper
					.Stub(arg => arg.MapAsync(Arg<Func<IContainer>>.Is.Anything, Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything))
					.Return(Task.Factory.Empty());
				_restrictionMapper1 = MockRepository.GenerateMock<IRestrictionMapper>();
				_restrictionMapper1
					.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything, Arg<IContainer>.Is.Anything))
					.WhenCalled(arg => ((Route.Routing.Route)arg.Arguments.Skip(2).First()).RestrictByMethods("GET").AsCompletedTask())
					.Return(Task.Factory.Empty());
				_restrictionMapper2 = MockRepository.GenerateMock<IRestrictionMapper>();
				_restrictionMapper2
					.Stub(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything, Arg<IContainer>.Is.Anything))
					.WhenCalled(arg => ((Route.Routing.Route)arg.Arguments.Skip(2).First()).RestrictByMethods("POST").AsCompletedTask())
					.Return(Task.Factory.Empty());
				_autoRouteCollection = new AutoRouteCollection()
					.Assemblies(Assembly.GetExecutingAssembly())
					.ClassFilters(_classFilter)
					.NameMappers(_nameMapper)
					.IdMappers(_idMapper)
					.ResolvedRelativeUrlMappers(_resolvedRelativeUrlMapper)
					.ResponseMapper(_responseMapper)
					.RestrictionMappers(_restrictionMapper1, _restrictionMapper2);
			}

			private AutoRouteCollection _autoRouteCollection;
			private IClassFilter _classFilter;
			private IIdMapper _idMapper;
			private INameMapper _nameMapper;
			private IResolvedRelativeUrlMapper _resolvedRelativeUrlMapper;
			private IResponseMapper _responseMapper;
			private Route.Routing.Route[] _routes;
			private IRestrictionMapper _restrictionMapper1;
			private IRestrictionMapper _restrictionMapper2;

			public class Endpoint
			{
				public void Method()
				{
				}
			}

			[Test]
			public async void Must_assign_mapped_restrictions()
			{
				var container = MockRepository.GenerateMock<IContainer>();

				_autoRouteCollection.RestrictionContainer(container);
				_routes = (await _autoRouteCollection.GenerateRouteCollectionAsync()).ToArray();

				MethodRestriction[] methodRestrictions = _routes[0].GetRestrictions<MethodRestriction>().ToArray();

				Assert.That(methodRestrictions, Has.Length.EqualTo(2));
			}

			[Test]
			public async void Must_map_using_all_mappers()
			{
				var container = MockRepository.GenerateMock<IContainer>();

				_autoRouteCollection.RestrictionContainer(container);
				await _autoRouteCollection.GenerateRouteCollectionAsync();

				_restrictionMapper1.AssertWasCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything, Arg<IContainer>.Is.Anything));
				_restrictionMapper1.AssertWasCalled(arg => arg.MapAsync(Arg<Type>.Is.Anything, Arg<MethodInfo>.Is.Anything, Arg<Route.Routing.Route>.Is.Anything, Arg<IContainer>.Is.Anything));
			}

			[Test]
			[ExpectedException(typeof(InvalidOperationException))]
#warning Update to use async Assert.That(..., Throws.Nothing) when NUnit 2.6.3 becomes available
			public async void Must_require_restriction_container()
			{
				await _autoRouteCollection.GenerateRouteCollectionAsync();
			}
		}
	}
}