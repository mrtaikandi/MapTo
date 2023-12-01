namespace MapTo.Mappings.Handlers;

internal class TypeConverterResolver
{
    private static readonly ITypeConverterResolver[] Resolvers =
    {
        new ImplicitTypeConverterResolver(),
        new ExplicitTypeConverterResolver(),
        new NestedTypeConverterResolver(),
        new EnumTypeConverterResolver(),
        new ArrayTypeConverterResolver(),
        new EnumerableTypeConverterResolver()
    };

    public static bool TryGet(MappingContext context, IPropertySymbol property, SourceProperty sourceProperty, out TypeConverterMapping converter)
    {
        converter = default;
        Diagnostic? error = null;

        foreach (var resolver in Resolvers)
        {
            var result = resolver.Get(context, property, sourceProperty);
            switch (result.Kind)
            {
                case HandlerResultKind.Undetermined:
                    error ??= result.Error;
                    break;

                case HandlerResultKind.Success:
                    converter = result.Value;
                    return true;

                case HandlerResultKind.Failure:
                    context.ReportDiagnostic(result.Error!);
                    return false;
            }
        }

        if (error is not null)
        {
            context.ReportDiagnostic(error);
        }

        return false;
    }
}