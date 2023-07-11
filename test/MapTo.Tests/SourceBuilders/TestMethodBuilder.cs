using System.Diagnostics.CodeAnalysis;
using MapTo.Configuration;
using MapTo.Extensions;
using MapTo.Generators;

namespace MapTo.Tests.SourceBuilders;

internal record TestMethodBuilder(
        string ReturnType,
        string Name,
        AccessModifier AccessModifier,
        bool Static,
        bool Async,
        bool Partial,
        [StringSyntax("csharp")] IEnumerable<string> Attributes,
        [StringSyntax("csharp")] IEnumerable<string> Parameters,
        [StringSyntax("csharp")] string Body)
    : ITestMethodBuilder
{
    /// <inheritdoc />
    public void Build(CodeWriter writer, TestSourceBuilderOptions options)
    {
        var parameters = string.Join(", ", Parameters);

        writer
            .WriteLines(Attributes)
            .Write(AccessModifier.ToLowercaseString())
            .WriteIf(Static, " static")
            .WriteIf(Async, " async")
            .WriteIf(Partial, " partial")
            .Write($" {ReturnType}")
            .Write($" {Name}(")
            .WriteIf(!string.IsNullOrWhiteSpace(parameters), parameters)
            .WriteLine(")")
            .WriteOpeningBracket();

        if (!string.IsNullOrWhiteSpace(Body))
        {
            // Break the body into lines and write each line individually to preserve indentation
            writer.WriteLines(Body.Split(writer.NewLine));
        }

        writer.WriteClosingBracket(); // Method closing bracket
    }
}

internal static class MethodBuilderExtensions
{
    internal static ITestClassBuilder WithMethod(
        this ITestClassBuilder builder,
        [StringSyntax("csharp")] string returnType,
        string name,
        [StringSyntax("csharp")] string body,
        AccessModifier accessModifier = AccessModifier.Public,
        bool isStatic = false,
        bool isAsync = false,
        bool isPartial = false,
        [StringSyntax("csharp")] IEnumerable<string>? attributes = null,
        [StringSyntax("csharp")] IEnumerable<string>? parameters = null)
    {
        builder.AddMember(
            new TestMethodBuilder(returnType, name, accessModifier, isStatic, isAsync, isPartial, attributes ?? Array.Empty<string>(), parameters ?? Array.Empty<string>(), body));

        return builder;
    }

    internal static ITestClassBuilder WithStaticMethod(
        this ITestClassBuilder builder,
        [StringSyntax("csharp")] string returnType,
        string name,
        [StringSyntax("csharp")] string body,
        AccessModifier accessModifier = AccessModifier.Public,
        bool isAsync = false,
        bool isPartial = false,
        [StringSyntax("csharp")] string? attribute = null,
        [StringSyntax("csharp")] string? parameter = null)
    {
        var attributes = attribute is null ? null : new[] { attribute };
        var parameters = parameter is null ? null : new[] { parameter };
        return builder.WithMethod(returnType, name, body, accessModifier, true, isAsync, isPartial, attributes, parameters);
    }

    internal static ITestClassBuilder WithStaticMethod(
        this ITestClassBuilder builder,
        [StringSyntax("csharp")] string returnType,
        string name,
        [StringSyntax("csharp")] string body,
        AccessModifier accessModifier = AccessModifier.Public,
        bool isAsync = false,
        bool isPartial = false,
        [StringSyntax("csharp")] IEnumerable<string>? attributes = null,
        [StringSyntax("csharp")] IEnumerable<string>? parameters = null) =>
        builder.WithMethod(returnType, name, body, accessModifier, true, isAsync, isPartial, attributes, parameters);

    internal static ITestClassBuilder WithStaticVoidMethod(
        this ITestClassBuilder builder,
        string name,
        [StringSyntax("csharp")] string body,
        AccessModifier accessModifier = AccessModifier.Public,
        bool isAsync = false,
        bool isPartial = false,
        [StringSyntax("csharp")] IEnumerable<string>? attributes = null,
        [StringSyntax("csharp")] IEnumerable<string>? parameters = null) =>
        builder.WithVoidMethod(name, body, accessModifier, true, isAsync, isPartial, attributes, parameters);

    internal static ITestClassBuilder WithVoidMethod(
        this ITestClassBuilder builder,
        string name,
        [StringSyntax("csharp")] string body,
        AccessModifier accessModifier = AccessModifier.Public,
        bool isStatic = false,
        bool isAsync = false,
        bool isPartial = false,
        [StringSyntax("csharp")] IEnumerable<string>? attributes = null,
        [StringSyntax("csharp")] IEnumerable<string>? parameters = null) =>
        builder.WithMethod("void", name, body, accessModifier, isStatic, isAsync, isPartial, attributes, parameters);
}