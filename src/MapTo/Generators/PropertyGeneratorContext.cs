using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct PropertyGeneratorContext(
    CodeWriter Writer,
    PropertyMapping PropertyMapping,
    string ContainingSourceTypeParameterName,
    string? TargetInstanceName,
    bool CopyPrimitiveArrays,
    string? ReferenceHandlerName);