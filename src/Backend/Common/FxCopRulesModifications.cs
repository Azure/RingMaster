// <copyright file="FxCopRulesModifications.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// Silencing code analysis warnings which would produce large diffs to fix
[module: SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Scope="Module", Justification = "Temporary silencing to enable code analysis")]
[module: SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1403:FileMayOnlyContainASingleNamespace", Scope="Module", Justification = "Temporary silencing to enable code analysis")]
[module: SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1201:ElementsMustAppearInTheCorrectOrder", Scope="Module", Justification = "Temporary silencing to enable code analysis")]
[module: SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Scope="Module", Justification = "Temporary silencing to enable code analysis")]
[module: SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Scope="Module", Justification = "Temporary silencing to enable code analysis")]