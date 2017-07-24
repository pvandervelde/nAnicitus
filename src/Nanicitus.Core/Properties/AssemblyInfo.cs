//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using Nuclei.Build;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCompany("nAnicitus")]
[assembly: AssemblyTrademark("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en-US")]

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Nanicitus.Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyProduct("Nanicitus.Core")]
[assembly: AssemblyCulture("")]

// The copyright
[assembly: AssemblyCopyright("Copyright P. van der Velde 2013 - ${CopyrightYear}")]

// The build configuration (e.g. debug/release)
[assembly: AssemblyConfiguration("${Configuration}")]

// The time the assembly was build
[assembly: AssemblyBuildTime(buildTime: "${BuildTime}")]

// The version from which the assembly was build
[module: SuppressMessage(
    "Microsoft.Usage",
    "CA2243:AttributeStringLiteralsShouldParseCorrectly",
    Justification = "It's a VCS revision, not a version you stupid FxCop")]
[assembly: AssemblyBuildInformation(buildNumber: ${BuildNumber}, versionControlInformation: "${VcsRevision}")]

// Version information for an assembly consists of the following four values:
//      Major Version
//      Minor Version
//      Build Number
//      Revision
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("${Major}.${Minor}.0.0")]
[assembly: AssemblyFileVersion("${Major}.${Minor}.${Patch}.${Build}")]

// The AssemblyInformationalVersion stores the version that will be displayed in
// Windows explorer.
[assembly: AssemblyInformationalVersion("${SemanticFull}")]
