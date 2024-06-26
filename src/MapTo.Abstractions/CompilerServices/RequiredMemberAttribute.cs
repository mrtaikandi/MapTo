#if NETSTANDARD2_0
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
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
internal class RequiredMemberAttribute : Attribute { }
#endif