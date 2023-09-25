namespace MapTo.Mappings;

internal readonly record struct ConstructorParameterMapping(string Name, ITypeSymbol Type, PropertyMapping Property, Location Location)
{
    public string TypeName => Type.ToDisplayString();
}