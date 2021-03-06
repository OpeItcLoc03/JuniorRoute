﻿using System.Collections.Generic;

using Junior.Common;
using Junior.Route.Common;
using Junior.Route.Diagnostics.Web;
using Junior.Route.Routing;

namespace Junior.Route.Diagnostics
{
	public class DiagnosticConfigurationRoutes
	{
		public static readonly DiagnosticConfigurationRoutes Instance = new DiagnosticConfigurationRoutes();

		private DiagnosticConfigurationRoutes()
		{
		}

		private static IEnumerable<string> DiagnosticsViewNamespaces
		{
			get
			{
				yield return "System.Linq";
			}
		}

		public IEnumerable<Routing.Route> GetRoutes(IGuidFactory guidFactory, IUrlResolver urlResolver, IHttpRuntime httpRuntime, string diagnosticsRelativeUrl, IEnumerable<IDiagnosticConfiguration> configurations)
		{
			guidFactory.ThrowIfNull("guidFactory");
			urlResolver.ThrowIfNull("urlResolver");
			diagnosticsRelativeUrl.ThrowIfNull("diagnosticsUrl");
			configurations.ThrowIfNull("configurations");

			string diagnosticsUrl = urlResolver.Absolute(diagnosticsRelativeUrl);

			yield return DiagnosticRouteHelper.Instance.GetViewRoute<DiagnosticsView>(
				"Diagnostics Home View",
				guidFactory,
				diagnosticsRelativeUrl,
				ResponseResources.Diagnostics,
				DiagnosticsViewNamespaces,
				httpRuntime,
				view =>
					{
						view.UrlResolver = urlResolver;
						AddLinks(view, diagnosticsUrl, configurations);
					});
			yield return DiagnosticRouteHelper.Instance.GetStylesheetRoute("Diagnostics Common CSS", guidFactory, diagnosticsRelativeUrl + "/css/common", ResponseResources.common, httpRuntime);
			yield return DiagnosticRouteHelper.Instance.GetStylesheetRoute("Diagnostics Reset CSS", guidFactory, diagnosticsRelativeUrl + "/css/reset", ResponseResources.reset, httpRuntime);
			yield return DiagnosticRouteHelper.Instance.GetJavaScriptRoute("Diagnostics jQuery JS", guidFactory, diagnosticsRelativeUrl + "/js/jquery", ResponseResources.jquery_1_8_2_min, httpRuntime);

			foreach (IDiagnosticConfiguration arg in configurations)
			{
				foreach (Routing.Route route in arg.GetRoutes(guidFactory, urlResolver, httpRuntime, diagnosticsRelativeUrl))
				{
					yield return route;
				}
			}
		}

		public IEnumerable<Routing.Route> GetRoutes(IGuidFactory guidFactory, IUrlResolver urlResolver, IHttpRuntime httpRuntime, string diagnosticsRelativeUrl, params IDiagnosticConfiguration[] configurations)
		{
			guidFactory.ThrowIfNull("guidFactory");
			diagnosticsRelativeUrl.ThrowIfNull("diagnosticsUrl");
			configurations.ThrowIfNull("configurations");

			return GetRoutes(guidFactory, urlResolver, httpRuntime, diagnosticsRelativeUrl, (IEnumerable<IDiagnosticConfiguration>)configurations);
		}

		private static void AddLinks(DiagnosticsView view, string diagnosticsUrl, IEnumerable<IDiagnosticConfiguration> configurations)
		{
			foreach (IDiagnosticConfiguration configuration in configurations)
			{
				view.AddDiagnosticViewLinks(configuration.GetLinks(diagnosticsUrl));
			}
		}
	}
}