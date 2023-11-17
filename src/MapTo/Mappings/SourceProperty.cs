namespace MapTo.Mappings;

internal readonly record struct SourceProperty(ITypeSymbol TypeSymbol, TypeMapping Type, string Name)
{
    internal bool NotFound => string.IsNullOrWhiteSpace(Name);
}