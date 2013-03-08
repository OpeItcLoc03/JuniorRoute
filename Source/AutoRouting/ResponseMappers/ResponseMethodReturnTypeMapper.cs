﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using Junior.Common;
using Junior.Route.AutoRouting.Containers;
using Junior.Route.AutoRouting.ParameterMappers;
using Junior.Route.AutoRouting.ParameterMappers.ModelPropertyMappers;
using Junior.Route.Common;
using Junior.Route.Routing.Responses;

using Newtonsoft.Json;

namespace Junior.Route.AutoRouting.ResponseMappers
{
	public class ResponseMethodReturnTypeMapper : IResponseMapper
	{
		private readonly HashSet<IParameterMapper> _parameterMappers = new HashSet<IParameterMapper>();

		public ResponseMethodReturnTypeMapper(IEnumerable<IParameterMapper> parameterMappers)
		{
			parameterMappers.ThrowIfNull("parameterMappers");

			_parameterMappers.AddRange(parameterMappers);
		}

		public ResponseMethodReturnTypeMapper(params IParameterMapper[] parameterMappers)
			: this((IEnumerable<IParameterMapper>)parameterMappers)
		{
		}

		public void Map(Func<IContainer> container, Type type, MethodInfo method, Routing.Route route)
		{
			container.ThrowIfNull("container");
			type.ThrowIfNull("type");
			method.ThrowIfNull("method");
			route.ThrowIfNull("route");

			if (method.ReturnType == typeof(void))
			{
				route.RespondWithNoContent();
				return;
			}

			bool methodReturnTypeImplementsIResponse = method.ReturnType.ImplementsInterface<IResponse>();
			bool methodReturnTypeIsTaskT = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

			if (methodReturnTypeImplementsIResponse)
			{
				ParameterInfo[] parameterInfos = method.GetParameters();
				ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(object), "instance");
				ParameterExpression parametersParameterExpression = Expression.Parameter(typeof(object[]), "parameters");
				UnaryExpression unaryExpression =
					Expression.Convert(
						Expression.Call(
							Expression.Convert(instanceParameterExpression, type),
							method,
							parameterInfos.Select((arg, index) => Expression.Convert(
								Expression.ArrayIndex(parametersParameterExpression, Expression.Constant(index)),
								arg.ParameterType))),
						typeof(IResponse));
				Func<object, object[], IResponse> @delegate = Expression.Lambda<Func<object, object[], IResponse>>(unaryExpression, instanceParameterExpression, parametersParameterExpression).Compile();

				route.RespondWith(
					request =>
						{
							object instance;

							try
							{
								instance = container().GetInstance(type);
							}
							catch (Exception exception)
							{
								throw new ApplicationException(String.Format("Unable to resolve instance of type {0}.", type.FullName), exception);
							}
							if (instance == null)
							{
								throw new ApplicationException(String.Format("Unable to resolve instance of type {0}.", type.FullName));
							}

							var parameterValueRetriever = new ParameterValueRetriever(_parameterMappers);
							object[] parameterValues = parameterValueRetriever.GetParameterValues(request, type, method).ToArray();

							return @delegate(instance, parameterValues);
						},
					method.ReturnType);
			}
			else if (methodReturnTypeIsTaskT)
			{
				ParameterInfo[] parameterInfos = method.GetParameters();
				ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(object), "instance");
				ParameterExpression parametersParameterExpression = Expression.Parameter(typeof(object[]), "parameters");
				Type methodGenericArgumentType = method.ReturnType.GetGenericArguments()[0];
				MethodInfo upcastMethodInfo = typeof(TaskExtensions)
					.GetMethod("Upcast", BindingFlags.Static | BindingFlags.Public)
					.MakeGenericMethod(methodGenericArgumentType, typeof(IResponse));
				UnaryExpression unaryExpression =
					Expression.Convert(
						Expression.Call(
							upcastMethodInfo,
							Expression.Call(
								Expression.Convert(instanceParameterExpression, type),
								method,
								parameterInfos.Select((arg, index) => Expression.Convert(
									Expression.ArrayIndex(parametersParameterExpression, Expression.Constant(index)),
									arg.ParameterType)))),
						upcastMethodInfo.ReturnType);
				Func<object, object[], Task<IResponse>> @delegate = Expression.Lambda<Func<object, object[], Task<IResponse>>>(unaryExpression, instanceParameterExpression, parametersParameterExpression).Compile();

				route.RespondWith(
					request =>
						{
							object instance;

							try
							{
								instance = container().GetInstance(type);
							}
							catch (Exception exception)
							{
								throw new ApplicationException(String.Format("Unable to resolve instance of type {0}.", type.FullName), exception);
							}
							if (instance == null)
							{
								throw new ApplicationException(String.Format("Unable to resolve instance of type {0}.", type.FullName));
							}

							var parameterValueRetriever = new ParameterValueRetriever(_parameterMappers);
							object[] parameterValues = parameterValueRetriever.GetParameterValues(request, type, method).ToArray();

							return @delegate(instance, parameterValues);
						},
					methodGenericArgumentType);
			}
			else
			{
				throw new ApplicationException(String.Format("The return type of {0}.{1} must implement {2} or be a {3} whose generic type argument implements {2}.", type.FullName, method.Name, typeof(IResponse).Name, typeof(Task<>)));
			}
		}

		public ResponseMethodReturnTypeMapper JsonModelMapper(
			Func<Type, bool> parameterTypeMatchDelegate,
			JsonSerializerSettings serializerSettings,
			DataConversionErrorHandling errorHandling = DataConversionErrorHandling.UseDefaultValue)
		{
			_parameterMappers.Add(new JsonModelMapper(parameterTypeMatchDelegate, serializerSettings, errorHandling));

			return this;
		}

		public ResponseMethodReturnTypeMapper JsonModelMapper(Func<Type, bool> parameterTypeMatchDelegate, DataConversionErrorHandling errorHandling = DataConversionErrorHandling.UseDefaultValue)
		{
			_parameterMappers.Add(new JsonModelMapper(parameterTypeMatchDelegate, errorHandling));

			return this;
		}

		public ResponseMethodReturnTypeMapper JsonModelMapper(JsonSerializerSettings serializerSettings, DataConversionErrorHandling errorHandling = DataConversionErrorHandling.UseDefaultValue)
		{
			_parameterMappers.Add(new JsonModelMapper(serializerSettings, errorHandling));

			return this;
		}

		public ResponseMethodReturnTypeMapper JsonModelMapper(DataConversionErrorHandling errorHandling = DataConversionErrorHandling.UseDefaultValue)
		{
			_parameterMappers.Add(new JsonModelMapper(errorHandling));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(IContainer container, Func<Type, bool> parameterTypeMatchDelegate, IEnumerable<IModelPropertyMapper> propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(container, parameterTypeMatchDelegate, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(IContainer container, Func<Type, bool> parameterTypeMatchDelegate, params IModelPropertyMapper[] propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(container, parameterTypeMatchDelegate, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(Func<Type, bool> parameterTypeMatchDelegate, IEnumerable<IModelPropertyMapper> propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(parameterTypeMatchDelegate, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(Func<Type, bool> parameterTypeMatchDelegate, params IModelPropertyMapper[] propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(parameterTypeMatchDelegate, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(IContainer container, IEnumerable<IModelPropertyMapper> propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(container, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(IContainer container, params IModelPropertyMapper[] propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(container, propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(IEnumerable<IModelPropertyMapper> propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(propertyMappers));

			return this;
		}

		public ResponseMethodReturnTypeMapper ModelMapper(params IModelPropertyMapper[] propertyMappers)
		{
			_parameterMappers.Add(new ModelMapper(propertyMappers));

			return this;
		}
	}
}