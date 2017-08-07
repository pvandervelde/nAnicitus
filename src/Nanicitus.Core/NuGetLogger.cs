//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Nuclei.Diagnostics;
using NuGet.Common;

namespace Nanicitus.Core
{
    internal sealed class NuGetLogger : ILogger
    {
        private SystemDiagnostics _diagnostics;

        public NuGetLogger(SystemDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public void LogDebug(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Debug,
                data);
        }

        public void LogError(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Error,
                data);
        }

        public void LogErrorSummary(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Error,
                data);
        }

        public void LogInformation(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Info,
                data);
        }

        public void LogInformationSummary(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Info,
                data);
        }

        public void LogMinimal(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Info,
                data);
        }

        public void LogVerbose(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Trace,
                data);
        }

        public void LogWarning(string data)
        {
            _diagnostics.Log(
                Nuclei.Diagnostics.Logging.LevelToLog.Warn,
                data);
        }
    }
}
