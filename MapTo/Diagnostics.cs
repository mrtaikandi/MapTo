using Microsoft.CodeAnalysis;

namespace MapTo
{
    internal static class Diagnostics
    {
        private const string UsageCategory = "Usage";

        internal static Diagnostic SymbolNotFound(Location location, string syntaxName) => 
            Diagnostic.Create(CreateDescriptor("MT0001", "Symbol not found.", $"Unable to find any symbols for {syntaxName}"), location);

        internal static Diagnostic MapFromAttributeNotFound(Location location) => 
            Diagnostic.Create(CreateDescriptor("MT0002", "Attribute Not Available", $"Unable to find {SourceBuilder.MapFromAttributeName} type."), location);

        internal static Diagnostic ClassMappingsGenerated(Location location, string typeName) =>
            Diagnostic.Create(CreateDescriptor("MT1001", "Mapped Type", $"Generated mappings for {typeName}", DiagnosticSeverity.Info), location);

        private static DiagnosticDescriptor CreateDescriptor(string id, string title, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
            new(id, title, message, UsageCategory, severity, true);
    }
}