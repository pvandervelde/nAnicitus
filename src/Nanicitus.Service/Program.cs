//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Topshelf;
using Topshelf.Autofac;

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
            DependencyInjection.CreateContainer();
            var host = HostFactory.New(
                c =>
                {
                    c.UseNLog();
                    c.UseAutofacContainer(DependencyInjection.Container);
                    c.Service<WebService>(
                        s =>
                        {
                            s.ConstructUsingAutofacContainer();
                            s.WhenStarted(a => a.Start());
                            s.WhenStopped(a => a.Stop());
                        });
                    c.StartAutomatically();

                    c.DependsOnEventLog();

                    c.EnableShutdown();
                });

            var exitCode = host.Run();
            return (exitCode == TopshelfExitCode.Ok) ? 0 : 1;
        }
    }
}
