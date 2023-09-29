namespace MapTo.Mappings;

internal readonly record struct ConstructorParameterMapping(string Name, TypeMapping Type, PropertyMapping Property, Location Location);