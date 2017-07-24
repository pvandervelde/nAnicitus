//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Nanicitus.Service.Properties;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace Nanicitus.Service
{
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.MaintainabilityRules",
        "SA1400:AccessModifierMustBeDeclared",
        Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <returns>An integer value indicating if the process exited without errors (return code 0), or not.</returns>
        static int Main()
        {
            var host = HostFactory.New(
                c =>
                {
                    c.Service(
                        (ServiceConfigurator<ServiceEntryPoint> s) =>
                        {
                            s.ConstructUsing(() => new ServiceEntryPoint());
                            s.WhenStarted(m => m.OnStart());
                            s.WhenStopped(m => m.OnStop());
                        });
                    c.StartAutomatically();

                    c.DependsOnEventLog();

                    c.EnableShutdown();

                    c.SetServiceName(Resources.Service_ServiceName);
                    c.SetDisplayName(Resources.Service_DisplayName);
                    c.SetDescription(Resources.Service_Description);
                });

            var exitCode = host.Run();
            return (exitCode == TopshelfExitCode.Ok) ? 0 : 1;
        }
    }
}
