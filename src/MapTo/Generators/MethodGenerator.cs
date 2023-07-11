using MapTo.Configuration;
using MapTo.Mappings;

namespace MapTo.Generators;

internal readonly record struct MethodGenerator(CodeGeneratorOptions Configuration, TargetMapping Mapping);