#if NETSTANDARD2_0
#pragma warning disable SA1623
// ReSharper disable UnusedType.Global
// ReSharper disable CheckNamespace
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string featureName)
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// The name of the compiler feature.
    /// </summary>
    public string FeatureName { get; }

    /// <summary>
    /// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// The <see cref="FeatureName"/> used for the ref structs C# feature.
    /// </summary>
    public const string RefStructs = nameof(RefStructs);

    /// <summary>
    /// The <see cref="FeatureName"/> used for the required members C# feature.
    /// </summary>
    public const string RequiredMembers = nameof(RequiredMembers);
}
#endif