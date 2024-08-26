namespace MapTo.Mappings.Handlers;

internal static class NestingTypeConverterResolverExtensions
{
    public static string ToExtensionClassName(this ITypeSymbol typeSymbol, MappingContext context)
    {
        if (typeSymbol.IsPrimitiveType())
        {
            return "System";
        }

        var ns = context.TargetTypeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var suffix = context.CodeGeneratorOptions.MapExtensionClassSuffix;

        return $"{ns}.{typeSymbol.Name}{suffix}";
    }
}