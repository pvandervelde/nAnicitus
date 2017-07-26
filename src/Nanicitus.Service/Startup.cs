//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Web.Http;
using System.Web.Http.Routing;
using Autofac.Integration.WebApi;
using Microsoft.Web.Http.Routing;
using Owin;

namespace Nanicitus.Service
{
    [SuppressMessage(
        "Microsoft.Performance",
        "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "This class is used by the WebAPI framework.")]
    internal sealed class Startup
    {
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This method is called by the WebAPI framework.")]
        public static void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            var constraintResolver = new DefaultInlineConstraintResolver()
            {
                ConstraintMap =
                {
                    ["apiVersion"] = typeof(ApiVersionRouteConstraint)
                }
            };
            config.MapHttpAttributeRoutes(constraintResolver);
            config.AddApiVersioning();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(DependencyInjection.Container);

            appBuilder.UseAutofacMiddleware(DependencyInjection.Container);
            appBuilder.UseAutofacWebApi(config);
            appBuilder.UseWebApi(config);
        }
    }
}
