using MapTo.Diagnostics;

namespace MapTo.Tests;

public class ReferenceHandlingTests
{
    private readonly ITestOutputHelper _output;

    public ReferenceHandlingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void When_PropertyTypeIsAnotherMappedClass_Should_MapToExtensionMethodToConvertType()
    {
        // Arrange
        var builder = new TestSourceBuilder();

        var nestedSourceFile = builder.AddFile();
        nestedSourceFile.AddClass(Accessibility.Public, "NestedSourceClass").WithProperty<int>("Id").WithProperty<string>("Name");
        nestedSourceFile.AddClass(Accessibility.Public, "NestedTargetClass", partial: true, attributes: "[MapFrom(typeof(NestedSourceClass))]")
            .WithProperty<int>("Id", propertyType: PropertyType.ReadOnly | PropertyType.AutoProperty)
            .WithProperty<string>("Name");

        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass").WithProperty<int>("Prop1").WithProperty("NestedSourceClass", "Prop2");
        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithProperty<int>("Prop1")
            .WithProperty("NestedTargetClass", "Prop2");

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        compilation.GetGeneratedFileSyntaxTree("MapTo.Tests.TargetClass.g.cs")
            .GetClassDeclaration("SourceClassToTargetClassMapToExtensions")
            .ShouldContain("Prop2 = global::MapTo.Tests.NestedSourceClassToNestedTargetClassMapToExtensions.MapToNestedTargetClass(sourceClass.Prop2)");
    }

    [Fact]
    public void When_ObjectIsSelfReferencingInConstructor_Should_ReportError()
    {
        // Arrange
        var builder = new TestSourceBuilder();

        var sourceFile = builder.AddFile();
        sourceFile.AddClass(Accessibility.Public, "SourceClass")
            .WithConstructor("public SourceClass(SourceClass prop1) => Prop1 = prop1;")
            .WithProperty("SourceClass", "Prop1");

        sourceFile.AddClass(Accessibility.Public, "TargetClass", partial: true, attributes: "[MapFrom(typeof(SourceClass))]")
            .WithConstructor("public TargetClass(TargetClass prop1) => Prop1 = prop1;")
            .WithProperty("TargetClass", "Prop1");

        // Act
        var (compilation, diagnostics) = builder.Compile(assertOutputCompilation: true);

        // Assert
        var targetClassDeclaration = compilation.GetClassDeclaration("TargetClass", "TestFile1.g.cs").ShouldNotBeNull();
        var constructorDeclarationSyntax = targetClassDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();
        var parameter = constructorDeclarationSyntax.ParameterList.Parameters.First();
        var diagnostic = DiagnosticsFactory.SelfReferencingConstructorMappingError(parameter.GetLocation(), "TargetClass");

        diagnostics.ShouldNotBeSuccessful(diagnostic);
    }

    [Theory]
    [InlineData(ReferenceHandling.Enabled)]
    [InlineData(ReferenceHandling.Auto)]
    public void When_ObjectIsSelfReferencingAndReferenceHandlingIsAutoOrEnable_Should_GenerateReferenceHandlingMethods(ReferenceHandling referenceHandling)
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(referenceHandling: referenceHandling);

        // Act
        var (compilation, diagnostics) = builder.Compile();

        // Assert
        diagnostics.ShouldBeSuccessful();
        var managerMapExtensionClass = compilation.GetClassDeclaration("ManagerToManagerViewModelMapToExtensions", "MapTo.Tests.ManagerViewModel.g.cs").ShouldNotBeNull();
        managerMapExtensionClass.ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("manager")]
            public static ManagerViewModel? MapToManagerViewModel(this Manager? manager)
            {
                var referenceHandler = new global::System.Collections.Generic.Dictionary<int, object>();
                return MapToManagerViewModel(manager, referenceHandler);
            }
            """);

        managerMapExtensionClass.ShouldContain(
            """
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("manager")]
            internal static ManagerViewModel? MapToManagerViewModel(Manager? manager, global::System.Collections.Generic.Dictionary<int, object> referenceHandler)
            {
                if (manager is null)
                 {
                     return null;
                 }

                 if (referenceHandler.TryGetValue(manager.GetHashCode(), out var cachedTarget))
                 {
                     return (ManagerViewModel)cachedTarget;
                 }

                 var target = new ManagerViewModel(manager.Id)
                 {
                     Level = manager.Level,
                     EmployeeCode = manager.EmployeeCode
                 };

                 referenceHandler.Add(manager.GetHashCode(), target);

                 target.Employees = manager.Employees?.Select(e => global::MapTo.Tests.EmployeeToEmployeeViewModelMapToExtensions.MapToEmployeeViewModel(e, referenceHandler)).ToList();

                 if (manager.Manager is not null)
                 {
                     target.Manager = global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(manager.Manager, referenceHandler);
                 }

                 return target;
            }
            """);
    }

    [Fact]
    public void When_ObjectIsSelfReferencing_Should_UseReferenceHandlingAndNotThrowStackOverflowException()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(referenceHandling: ReferenceHandling.Enabled);

        // Act
        var (_, diagnostics) = builder.ExecuteAndAssertDynamicCode(
            dynamicClassName: "CyclicReferenceTestsRunner",
            logger: _output,
            code: """
                  var manager = new Manager { Id = 1, EmployeeCode = "M001", Level = 100 };
                  manager.Manager = manager;
                  var result = manager.MapToManagerViewModel();
                  """);

        // Assert
        diagnostics.ShouldBeSuccessful();
    }

    [Fact]
    public void When_ReferenceHandlingIsDisabledViaMapToAttribute_Should_NotGenerateReferenceHandlerMethod()
    {
        // Arrange
        var builder = ScenarioBuilder.BuildEmployeeManagerModels(referenceHandling: ReferenceHandling.Disabled);

        // Act
        var (compilation, diagnostics) = builder.Compile();
        compilation.Dump(_output);

        // Assert
        diagnostics.ShouldBeSuccessful();

        var managerExtensionClass = compilation.GetClassDeclaration("ManagerToManagerViewModelMapToExtensions", "MapTo.Tests.ManagerViewModel.g.cs");
        managerExtensionClass.ShouldNotContain("""
                                               return new ManagerViewModel(manager.Id)
                                               {
                                                   Level = manager.Level,
                                                   Employees = manager.Employees.Select(MapTo.Tests.EmployeeMapToExtensions.MapToEmployeeViewModel).ToList(),
                                                   EmployeeCode = manager.EmployeeCode,
                                                   Manager = MapTo.Tests.ManagerMapToExtensions.MapToManagerViewModel(manager.Manager)
                                               };
                                               """);

        managerExtensionClass.ShouldNotContain(
            "internal static ManagerViewModel? MapToManagerViewModel(Manager? manager, global::System.Collections.Generic.Dictionary<int, object> referenceHandler)");

        var employeeExtensionClass = compilation.GetClassDeclaration("EmployeeToEmployeeViewModelMapToExtensions", "MapTo.Tests.EmployeeViewModel.g.cs");
        employeeExtensionClass.ShouldContain(
            """
            var target = new EmployeeViewModel
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode
            };

            if (employee.Manager is not null)
            {
                target.Manager = global::MapTo.Tests.ManagerToManagerViewModelMapToExtensions.MapToManagerViewModel(employee.Manager);
            }

            return target;
            """);

        employeeExtensionClass.ShouldNotContain(
            "internal static EmployeeViewModel? MapToEmployeeViewModel(Employee? employee, global::System.Collections.Generic.Dictionary<int, object> referenceHandler)");
    }
}