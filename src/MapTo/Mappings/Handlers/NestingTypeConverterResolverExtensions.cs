namespace MapTo.Mappings.Handlers;

internal static class NestingTypeConverterResolverExtensions
{
    public static string ToExtensionClassName(this INamedTypeSymbol typeSymbol, CodeGeneratorOptions options) => typeSymbol.IsPrimitiveType()
        ? "System"
        : $"{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{options.MapExtensionClassSuffix}";
}