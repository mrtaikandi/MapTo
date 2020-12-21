using Microsoft.CodeAnalysis;

namespace MapTo
{
    internal static class Diagnostics
    {
        private const string UsageCategory = "Usage";

        internal static Diagnostic SymbolNotFound(Location location, string syntaxName) =>
            Create("MT0001", "Symbol not found.", $"Unable to find any symbols for {syntaxName}", location);

        internal static Diagnostic MapFromAttributeNotFound(Location location) =>
            Create("MT0002", "Attribute Not Available", $"Unable to find {SourceBuilder.MapFromAttributeName} type.", location);

        internal static Diagnostic ClassMappingsGenerated(Location location, string typeName) =>
            Create("MT1001", "Mapped Type", $"Generated mappings for {typeName}", location, DiagnosticSeverity.Info);

        private static Diagnostic Create(string id, string title, string message, Location location, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
            Diagnostic.Create(new DiagnosticDescriptor(id, title, message, UsageCategory, severity, true), location);
    }
}