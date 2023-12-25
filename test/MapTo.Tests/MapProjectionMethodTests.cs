namespace MapTo.Tests;

public class MapProjectionMethodTests
{
    private readonly ITestOutputHelper _output;

    public MapProjectionMethodTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_ProjectionIsAvailable_Should_GeneratePartialMethod()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordWithProjectionMethod(
            method: "internal static partial DestinationRecord[]? MapToDestinationRecordArray(SourceRecord[]? sourceRecords);",
            supportNullReferenceTypes: true);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetSyntaxTreeContentByFileName("MapTo.Tests.DestinationRecord")
            .ShouldNotBeNull()
            .ShouldContain(
                $$"""
                  {{ScenarioBuilder.GeneratedCodeAttribute}}
                  public partial record DestinationRecord
                  {
                      [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceRecords")]
                      internal static partial global::MapTo.Tests.DestinationRecord[]? MapToDestinationRecordArray(global::MapTo.Tests.SourceRecord[]? sourceRecords)
                      {
                          return global::MapTo.Tests.SourceRecordMapToExtensions.MapToDestinationRecordArray(sourceRecords);
                      }
                  }
                  """);
    }

    [Fact]
    public void When_ProjectionIsNullableArray_Should_GenerateProjectionExtensionMethods()
    {
        // Arrange
        var builder = ScenarioBuilder.SimpleMappedRecordWithProjectionMethod(
            method: "internal static partial DestinationRecord[]? MapToDestinationRecordArray(SourceRecord[]? sourceRecords);",
            supportNullReferenceTypes: true);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetClassDeclaration("SourceRecordMapToExtensions").ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("sourceRecords")]
            internal static global::MapTo.Tests.DestinationRecord[]? MapToDestinationRecordArray(this global::MapTo.Tests.SourceRecord[]? sourceRecords)
            {
                if (sourceRecords is null)
                {
                    return null;
                }
            
                var target = new global::MapTo.Tests.DestinationRecord[sourceRecords.Length];
                for (var i = 0; i < sourceRecords.Length; i++)
                {
                    target[i] = global::MapTo.Tests.SourceRecordMapToExtensions.MapToDestinationRecord(sourceRecords[i]);
                }
            
                return target;
            }
            """);
    }

    [Fact]
    public void When_MultipleProjectionIsAvailable_Should_GenerateProjectionExtensionMethodForEach()
    {
        // Arrange
        var builder = new TestSourceBuilder(TestSourceBuilderOptions.Create(supportNullReferenceTypes: true));
        builder.AddFile(
                supportNullableReferenceTypes: true,
                usings: new[] { "System", "System.Collections.Generic", "System.Collections.Immutable", "System.Collections.ObjectModel" })
            .WithBody(
                """
                public record SourceRecord(int Value);

                [MapFrom(typeof(SourceRecord), ProjectTo = ProjectionType.None)]
                public partial record DestinationRecord(int Value)
                {
                    internal static partial DestinationRecord[] MapToDestinationRecordArray(SourceRecord[] sourceRecords);
                    private static partial IEnumerable<DestinationRecord> MapToDestinationRecordEnumerable(IEnumerable<SourceRecord> sourceRecords);
                }
                """);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();

        var destinationFileSyntax = compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.DestinationRecord").ShouldNotBeNull();
        var partialRecordSyntax = destinationFileSyntax.GetRoot().DescendantNodes()
            .OfType<RecordDeclarationSyntax>()
            .SingleOrDefault(r => r.Identifier.Text == "DestinationRecord")
            .ShouldNotBeNull();

        var methods = partialRecordSyntax.Members.OfType<MethodDeclarationSyntax>().ToArray();
        methods.Length.ShouldBe(2);
        methods[0].Identifier.Text.ShouldBe("MapToDestinationRecordArray");
        methods[0].Body?.Statements.ToString().ShouldBe("return global::MapTo.Tests.SourceRecordMapToExtensions.MapToDestinationRecordArray(sourceRecords);");
        methods[1].Identifier.Text.ShouldBe("MapToDestinationRecordEnumerable");
        methods[1].Body?.Statements.ToString().ShouldBe("return global::MapTo.Tests.SourceRecordMapToExtensions.MapToDestinationRecordEnumerable(sourceRecords);");

        var extensionClass = compilation.GetClassDeclaration("SourceRecordMapToExtensions").ShouldNotBeNull();
        var extensionMethods = extensionClass.Members.OfType<MethodDeclarationSyntax>().ToArray();
        extensionMethods.Length.ShouldBe(3);
        extensionMethods[1].Identifier.Text.ShouldBe("MapToDestinationRecordArray");
        extensionMethods[1].Modifiers.ToString().ShouldBe("internal static");
        extensionMethods[2].Identifier.Text.ShouldBe("MapToDestinationRecordEnumerable");
        extensionMethods[2].Modifiers.ToString().ShouldBe("internal static");
    }

    [Theory]
    [InlineData("[]", "[]")]
    [InlineData("[]", "List")]
    [InlineData("[]", "IList")]
    [InlineData("[]", "ICollection")]
    [InlineData("[]", "IReadOnlyList")]
    [InlineData("[]", "IReadOnlyCollection")]
    [InlineData("[]", "Span")]
    [InlineData("[]", "Memory")]
    [InlineData("[]", "IEnumerable")]
    [InlineData("[]", "ImmutableArray")]
    [InlineData("List", "[]")]
    [InlineData("List", "List")]
    [InlineData("List", "IList")]
    [InlineData("List", "ICollection")]
    [InlineData("List", "IReadOnlyList")]
    [InlineData("List", "IReadOnlyCollection")]
    [InlineData("List", "Span")]
    [InlineData("List", "Memory")]
    [InlineData("List", "ImmutableArray")]
    [InlineData("List", "IEnumerable")]
    [InlineData("IList", "[]")]
    [InlineData("IList", "List")]
    [InlineData("IList", "ICollection")]
    [InlineData("IList", "IReadOnlyList")]
    [InlineData("IList", "IReadOnlyCollection")]
    [InlineData("IList", "Span")]
    [InlineData("IList", "Memory")]
    [InlineData("IList", "ImmutableArray")]
    [InlineData("IList", "IEnumerable")]
    [InlineData("ICollection", "[]")]
    [InlineData("ICollection", "List")]
    [InlineData("ICollection", "IList")]
    [InlineData("ICollection", "IReadOnlyList")]
    [InlineData("ICollection", "IReadOnlyCollection")]
    [InlineData("ICollection", "Span")]
    [InlineData("ICollection", "Memory")]
    [InlineData("ICollection", "ImmutableArray")]
    [InlineData("ICollection", "IEnumerable")]
    [InlineData("IReadOnlyList", "[]")]
    [InlineData("IReadOnlyList", "List")]
    [InlineData("IReadOnlyList", "IList")]
    [InlineData("IReadOnlyList", "ICollection")]
    [InlineData("IReadOnlyList", "IReadOnlyCollection")]
    [InlineData("IReadOnlyList", "Span")]
    [InlineData("IReadOnlyList", "Memory")]
    [InlineData("IReadOnlyList", "ImmutableArray")]
    [InlineData("IReadOnlyList", "IEnumerable")]
    [InlineData("IReadOnlyCollection", "[]")]
    [InlineData("IReadOnlyCollection", "List")]
    [InlineData("IReadOnlyCollection", "IList")]
    [InlineData("IReadOnlyCollection", "ICollection")]
    [InlineData("IReadOnlyCollection", "IReadOnlyList")]
    [InlineData("IReadOnlyCollection", "Span")]
    [InlineData("IReadOnlyCollection", "Memory")]
    [InlineData("IReadOnlyCollection", "ImmutableArray")]
    [InlineData("IReadOnlyCollection", "IEnumerable")]
    [InlineData("Span", "[]")]
    [InlineData("Span", "List")]
    [InlineData("Span", "IList")]
    [InlineData("Span", "ICollection")]
    [InlineData("Span", "IReadOnlyList")]
    [InlineData("Span", "IReadOnlyCollection")]
    [InlineData("Span", "Memory")]
    [InlineData("Span", "ImmutableArray")]
    [InlineData("Span", "IEnumerable")]
    [InlineData("Memory", "[]")]
    [InlineData("Memory", "List")]
    [InlineData("Memory", "IList")]
    [InlineData("Memory", "ICollection")]
    [InlineData("Memory", "IReadOnlyList")]
    [InlineData("Memory", "IReadOnlyCollection")]
    [InlineData("Memory", "Span")]
    [InlineData("Memory", "ImmutableArray")]
    [InlineData("Memory", "IEnumerable")]
    [InlineData("ImmutableArray", "[]")]
    [InlineData("ImmutableArray", "List")]
    [InlineData("ImmutableArray", "IList")]
    [InlineData("ImmutableArray", "ICollection")]
    [InlineData("ImmutableArray", "IReadOnlyList")]
    [InlineData("ImmutableArray", "IReadOnlyCollection")]
    [InlineData("ImmutableArray", "Span")]
    [InlineData("ImmutableArray", "Memory")]
    [InlineData("ImmutableArray", "IEnumerable")]
    [InlineData("IEnumerable", "[]")]
    [InlineData("IEnumerable", "IEnumerable")]
    [InlineData("IEnumerable", "List")]
    [InlineData("IEnumerable", "IList")]
    [InlineData("IEnumerable", "ICollection")]
    [InlineData("IEnumerable", "IReadOnlyList")]
    [InlineData("IEnumerable", "IReadOnlyCollection")]
    [InlineData("IEnumerable", "Span")]
    [InlineData("IEnumerable", "Memory")]
    [InlineData("IEnumerable", "ImmutableArray")]
    [InlineData("ReadOnlySpan", "[]")]
    [InlineData("ReadOnlySpan", "List")]
    [InlineData("ReadOnlySpan", "IList")]
    [InlineData("ReadOnlySpan", "ICollection")]
    [InlineData("ReadOnlySpan", "IReadOnlyList")]
    [InlineData("ReadOnlySpan", "IReadOnlyCollection")]
    [InlineData("ReadOnlySpan", "Span")]
    [InlineData("ReadOnlySpan", "Memory")]
    [InlineData("ReadOnlySpan", "ImmutableArray")]
    [InlineData("ReadOnlySpan", "IEnumerable")]
    [InlineData("ReadOnlySpan", "ReadOnlySpan")]
    [InlineData("ReadOnlySpan", "ReadOnlyMemory")]
    [InlineData("ReadOnlyMemory", "[]")]
    [InlineData("ReadOnlyMemory", "List")]
    [InlineData("ReadOnlyMemory", "IList")]
    [InlineData("ReadOnlyMemory", "ICollection")]
    [InlineData("ReadOnlyMemory", "IReadOnlyList")]
    [InlineData("ReadOnlyMemory", "IReadOnlyCollection")]
    [InlineData("ReadOnlyMemory", "Span")]
    [InlineData("ReadOnlyMemory", "Memory")]
    [InlineData("ReadOnlyMemory", "ImmutableArray")]
    [InlineData("ReadOnlyMemory", "IEnumerable")]
    [InlineData("ReadOnlyMemory", "ReadOnlySpan")]
    [InlineData("ReadOnlyMemory", "ReadOnlyMemory")]
    public void When_MapProjectionMethodExists_Should_GenerateExtensionMethod(string returnType, string parameterType)
    {
        // Arrange
        const string TestNamespace = "global::MapTo.Tests";
        const string ReturnElement = $"{TestNamespace}.DestinationRecord";
        const string EnumerableType = $"global::{KnownTypes.LinqEnumerable}";
        const string MapMethodName = $"{TestNamespace}.SourceRecordMapToExtensions.MapToDestinationRecord";

        var expectedParameterType = parameterType switch
        {
            "[]" => $"{TestNamespace}.SourceRecord[]",
            "ImmutableArray" => $"global::System.Collections.Immutable.ImmutableArray<{TestNamespace}.SourceRecord>",
            "Span" => $"global::System.Span<{TestNamespace}.SourceRecord>",
            "ReadOnlySpan" => $"global::System.ReadOnlySpan<{TestNamespace}.SourceRecord>",
            "Memory" => $"global::System.Memory<{TestNamespace}.SourceRecord>",
            "ReadOnlyMemory" => $"global::System.ReadOnlyMemory<{TestNamespace}.SourceRecord>",
            _ => $"global::System.Collections.Generic.{parameterType}<{TestNamespace}.SourceRecord>"
        };

        var expectedReturnType = returnType switch
        {
            "[]" => $"{ReturnElement}[]",
            "Span" => $"global::System.Span<{ReturnElement}>",
            "Memory" => $"global::System.Memory<{ReturnElement}>",
            "ImmutableArray" => $"global::System.Collections.Immutable.ImmutableArray<{ReturnElement}>",
            "ReadOnlyMemory" => $"global::System.ReadOnlyMemory<{ReturnElement}>",
            "ReadOnlySpan" => $"global::System.ReadOnlySpan<{ReturnElement}>",
            _ => $"global::System.Collections.Generic.{returnType}<{ReturnElement}>"
        };

        var isParameterFixedSize = new[] { "[]", "Span", "Memory", "ImmutableArray", "ReadOnlySpan", "ReadOnlyMemory" }.Contains(parameterType);
        var parameterSizeProperty = isParameterFixedSize ? "Length" : "Count";
        var parameterName = new[] { "Memory", "ReadOnlyMemory" }.Contains(parameterType) ? "sourceRecords.Span" : "sourceRecords";
        var expectedTargetVariable = returnType switch
        {
            "IList" => $"new global::System.Collections.Generic.List<{ReturnElement}>({parameterName}.{parameterSizeProperty})",
            "List" => $"new global::System.Collections.Generic.List<{ReturnElement}>({parameterName}.{parameterSizeProperty})",
            "IReadOnlyList" => $"new global::System.Collections.Generic.List<{ReturnElement}>({parameterName}.{parameterSizeProperty})",
            "ICollection" => $"new global::System.Collections.Generic.List<{ReturnElement}>({parameterName}.{parameterSizeProperty})",
            "IReadOnlyCollection" => $"new global::System.Collections.Generic.List<{ReturnElement}>({parameterName}.{parameterSizeProperty})",
            _ => $"new {ReturnElement}[{parameterName}.{parameterSizeProperty}]"
        };

        var hasForeachLoop = new[] { "ICollection", "IReadOnlyCollection" }.Contains(parameterType) && returnType != "ImmutableArray";
        var hasForLoop = !hasForeachLoop && !new[] { "IEnumerable" }.Contains(parameterType) && returnType != "ImmutableArray";
        var parameterItem = hasForeachLoop ? "item" : $"{parameterName}[i]";
        var expectedLoopBody = returnType switch
        {
            "IList" => $"target.Add({MapMethodName}({parameterItem}));",
            "List" => $"target.Add({MapMethodName}({parameterItem}));",
            "IReadOnlyList" => $"target.Add({MapMethodName}({parameterItem}));",
            "ICollection" => $"target.Add({MapMethodName}({parameterItem}));",
            "IReadOnlyCollection" => $"target.Add({MapMethodName}({parameterItem}));",
            _ => $"target[i] = {MapMethodName}({parameterItem});"
        };

        // For when it's not a loop
        var expectedReturnStatement = returnType switch
        {
            "[]" => $"return {EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "List" => $"return {EnumerableType}.ToList({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "IList" => $"return {EnumerableType}.ToList({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "IEnumerable" => $"return {EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x));",
            "ICollection" => $"return {EnumerableType}.ToList({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "IReadOnlyList" => $"return {EnumerableType}.ToList({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "IReadOnlyCollection" => $"return {EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            "Span" => $"return new global::System.Span<{ReturnElement}>({EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x))));",
            "ReadOnlySpan" =>
                $"return new global::System.ReadOnlySpan<{ReturnElement}>({EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x))));",
            "Memory" => $"return new global::System.Memory<{ReturnElement}>({EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x))));",
            "ReadOnlyMemory" =>
                $"return new global::System.ReadOnlyMemory<{ReturnElement}>({EnumerableType}.ToArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x))));",
            "ImmutableArray" when parameterType == "Memory" =>
                $"return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({EnumerableType}.Select(sourceRecords.Span.ToArray(), x => {MapMethodName}(x)));",
            "ImmutableArray" when parameterType == "Span" =>
                $"return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({EnumerableType}.Select(sourceRecords.ToArray(), x => {MapMethodName}(x)));",
            "ImmutableArray" => $"return global::System.Collections.Immutable.ImmutableArray.ToImmutableArray({EnumerableType}.Select(sourceRecords, x => {MapMethodName}(x)));",
            _ => throw new InvalidOperationException("Unknown enumerable type.")
        };

        var partialMethodReturnType = returnType == "[]" ? "DestinationRecord[]" : $"{returnType}<DestinationRecord>";
        var partialMethodParameterType = parameterType == "[]" ? "SourceRecord[]" : $"{parameterType}<SourceRecord>";
        var builder = ScenarioBuilder.SimpleMappedRecordWithProjectionMethod(
            method: $"internal static partial {partialMethodReturnType} MapProjectionTest({partialMethodParameterType} sourceRecords);",
            supportNullReferenceTypes: true);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        compilation.Dump(_output);
        diagnostics.ShouldBeSuccessful();
        var extensionClass = compilation.GetClassDeclaration("SourceRecordMapToExtensions").ShouldNotBeNull();

        var projectionMethod = extensionClass.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(m => m.Identifier.Text == "MapProjectionTest").ShouldNotBeNull();
        projectionMethod.ReturnType.ToString().ShouldBe(expectedReturnType);
        projectionMethod.ParameterList.Parameters.Count.ShouldBe(1);
        projectionMethod.ParameterList.Parameters[0].Type?.ToString().ShouldBe(expectedParameterType);

        var projectionBody = projectionMethod.Body.ShouldNotBeNull();
        if (hasForLoop)
        {
            projectionBody.Statements.Count.ShouldBe(3);
            projectionBody.Statements[0].ToString().ShouldBe($"var target = {expectedTargetVariable};");

            var loopStatement = projectionBody.Statements[1].ShouldBeOfType<ForStatementSyntax>().ShouldNotBeNull();
            loopStatement.Condition?.ToString().ShouldBe($"i < {parameterName}.{parameterSizeProperty}");
            loopStatement.Incrementors.Count.ShouldBe(1);
            loopStatement.Incrementors[0].ToString().ShouldBe("i++");
            loopStatement.Declaration?.ToString().ShouldBe("var i = 0");

            var loopBody = loopStatement.Statement.ShouldBeOfType<BlockSyntax>().ShouldNotBeNull();
            loopBody.Statements.Count.ShouldBe(1);
            loopBody.Statements[0].ToString().ShouldBe(expectedLoopBody);

            projectionBody.Statements[2].ToString().ShouldBe("return target;");
        }
        else if (hasForeachLoop)
        {
            projectionBody.Statements.Count.ShouldBe(4);
            projectionBody.Statements[0].ToString().ShouldBe($"var target = {expectedTargetVariable};");
            projectionBody.Statements[1].ToString().ShouldBe("var i = 0;");

            var loopStatement = projectionBody.Statements[2].ShouldBeOfType<ForEachStatementSyntax>().ShouldNotBeNull();
            loopStatement.Expression?.ToString().ShouldBe(parameterName);
            loopStatement.Identifier.ToString().ShouldBe("item");

            var loopBody = loopStatement.Statement.ShouldBeOfType<BlockSyntax>().ShouldNotBeNull();
            loopBody.Statements.Count.ShouldBe(2);
            loopBody.Statements[0].ToString().ShouldBe(expectedLoopBody);
            loopBody.Statements[1].ToString().ShouldBe("i++;");

            projectionBody.Statements[3].ToString().ShouldBe("return target;");
        }
        else
        {
            projectionBody.Statements.Count.ShouldBe(1);
            projectionBody.Statements[0].ToString().ShouldBe(expectedReturnStatement);
        }
    }
}