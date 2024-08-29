namespace MapTo.Mappings.Handlers;

internal static class NestingTypeConverterResolverExtensions
{
    public static string ToExtensionClassName(this TypeMapping type, MappingContext context)
    {
        if (type.IsPrimitive)
        {
            return "System";
        }

        var ns = context.TargetTypeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var suffix = context.CodeGeneratorOptions.MapExtensionClassSuffix;

        return $"{ns}.{type.Name}{suffix}";
    }
}