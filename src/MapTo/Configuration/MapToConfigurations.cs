using System;
using Microsoft.CodeAnalysis;

namespace MapTo.Configuration
{
    internal sealed class MapToConfigurations
    {
        private MapToConfigurations(AccessModifier constructorAccessModifier)
        {
            ConstructorAccessModifier = constructorAccessModifier;
        }

        internal AccessModifier ConstructorAccessModifier { get; }

        internal static MapToConfigurations From(GeneratorExecutionContext context)
        {
            var constructorAccessModifier = 
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MapTo_ConstructorAccessModifier", out var ctorModifierValue) && 
                Enum.TryParse<AccessModifier>(ctorModifierValue, out var ctorModifier) ? ctorModifier : AccessModifier.Public;

            return new MapToConfigurations(
                constructorAccessModifier
            );
        }
    }
}