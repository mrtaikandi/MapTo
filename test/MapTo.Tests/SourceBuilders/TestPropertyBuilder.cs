using System.Diagnostics.CodeAnalysis;
using MapTo.Configuration;
using MapTo.Extensions;
using MapTo.Generators;

namespace MapTo.Tests.SourceBuilders;

internal record TestPropertyBuilder(string Type, string Name, AccessModifier AccessModifier, PropertyType PropertyType, IEnumerable<string>? Attributes)
    : ITestPropertyBuilder
{
    /// <inheritdoc />
    public void Build(CodeWriter writer, TestSourceBuilderOptions options)
    {
        var attributes = Attributes?.ToArray() ?? Array.Empty<string>();
        var backingField = $"_{Name.ToParameterNameCasing()}";
        var hasBackingField = PropertyType.HasFlag(PropertyType.PropertyWithBackingField);
        var autoProperty = PropertyType.HasFlag(PropertyType.AutoProperty);
        var initProperty = PropertyType.HasFlag(PropertyType.InitProperty);
        var readOnly = PropertyType.HasFlag(PropertyType.ReadOnly);

        writer.WriteLineIf(hasBackingField, $"private {Type} {backingField};");
        writer.WriteLineIf(hasBackingField && attributes.Any());

        foreach (var attribute in attributes)
        {
            writer.WriteLine(attribute);
        }

        if (autoProperty)
        {
            writer
                .Write($"{AccessModifier.ToLowercaseString()} {Type} {Name}")
                .Write(" {")
                .Write(" get;")
                .WriteIf(initProperty, " init;")
                .WriteIf(!readOnly && !initProperty, " set;")
                .WriteLine(" }");
        }
        else
        {
            writer
                .WriteLine($"{AccessModifier.ToLowercaseString()} {Type} {Name}")
                .WriteOpeningBracket()
                .WriteLine($"get => {backingField};")
                .WriteLineIf(!readOnly, $"set => {backingField} = value;")
                .WriteClosingBracket();
        }
    }
}

internal static class PropertyBuilderExtensions
{
    internal static ITestClassBuilder AddPublicReadOnlyProperties(this ITestClassBuilder builder, params (string Type, string Name)[] properties)
    {
        foreach (var (type, name) in properties)
        {
            builder.WithProperty(type, name, AccessModifier.Public, PropertyType.ReadOnly & PropertyType.AutoProperty);
        }

        return builder;
    }

    internal static string GetFriendlyName(this Type type) => type switch
    {
        _ when type == typeof(int) => "int",
        _ when type == typeof(short) => "short",
        _ when type == typeof(long) => "long",
        _ when type == typeof(byte) => "byte",
        _ when type == typeof(float) => "float",
        _ when type == typeof(double) => "double",
        _ when type == typeof(decimal) => "decimal",
        _ when type == typeof(bool) => "bool",
        _ when type == typeof(char) => "char",
        _ when type == typeof(string) => "string",
        _ when type == typeof(object) => "object",
        { IsGenericType: true } when type.GetGenericTypeDefinition() == typeof(Nullable<>) => GetFriendlyName(type.GetGenericArguments()[0]) + "?",
        _ => type.Name
    };

    internal static ITestClassBuilder WithProperty<T>(
        this ITestClassBuilder builder,
        string name,
        AccessModifier accessModifier = AccessModifier.Public,
        PropertyType propertyType = PropertyType.AutoProperty,
        [StringSyntax("csharp")] params string[] attributes) =>
        builder.WithProperty(typeof(T).GetFriendlyName(), name, accessModifier, propertyType, attributes);

    internal static ITestClassBuilder WithProperty(
        this ITestClassBuilder builder,
        string type,
        string name,
        AccessModifier accessModifier = AccessModifier.Public,
        PropertyType propertyType = PropertyType.AutoProperty,
        [StringSyntax("csharp")] params string[] attributes) =>
        WithProperty(builder, new TestPropertyBuilder(type, name, accessModifier, propertyType, attributes));

    internal static ITestClassBuilder WithProperty(this ITestClassBuilder builder, TestPropertyBuilder propertyBuilder)
    {
        builder.AddMember(propertyBuilder);
        return builder;
    }
}