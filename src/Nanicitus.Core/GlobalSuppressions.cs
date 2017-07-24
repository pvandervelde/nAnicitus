//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Microsoft.Naming",
    "CA1703:ResourceStringsShouldBeSpelledCorrectly",
    MessageId = "Str",
    Scope = "resource",
    Target = "Nanicitus.Core.Properties.Resources.resources",
    Justification = "This is part of the PdbStr application name.")]
[assembly: SuppressMessage(
    "Microsoft.Naming",
    "CA1703:ResourceStringsShouldBeSpelledCorrectly",
    MessageId = "Sym",
    Scope = "resource",
    Target = "Nanicitus.Core.Properties.Resources.resources",
    Justification = "This is part of the SymStore application name.")]
