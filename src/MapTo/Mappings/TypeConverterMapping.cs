using System.Diagnostics;
using MapTo.Extensions;

namespace MapTo.Mappings;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly record struct TypeConverterMapping(
    MethodMapping Method,
    bool Explicit,
    string? Parameter = null,
    bool IsMapToExtensionMethod = false,
    bool ReferenceHandling = false,
    EnumTypeMapping EnumMapping = default)
{
    public TypeConverterMapping(IMethodSymbol method, TypedConstant parameters)
        : this(
            Method: new MethodMapping(method),
            Parameter: parameters.IsNull ? null : parameters.ToSourceCodeString(),
            Explicit: true) { }

    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool HasParameter => Parameter is not null;

    public string ContainingType => Method.ContainingType;

    public string MethodName => Method.MethodName;

    public TypeMapping Type => Method.ReturnType;

    public string MethodFullName => string.IsNullOrEmpty(ContainingType) ? MethodName : $"{ContainingType}.{MethodName}";

#if DEBUG
    private string GetDebuggerDisplay() => this == default ? "None" : ToString();
#endif
}