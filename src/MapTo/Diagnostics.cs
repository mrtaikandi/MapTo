using MapTo.Sources;
using Microsoft.CodeAnalysis;

namespace MapTo
{
    internal static class Diagnostics
    {
        private const string UsageCategory = "Usage";
        private const string ErrorId = "MT0";
        private const string InfoId = "MT1";
        private const string WarningId = "MT2";
        
        internal static Diagnostic SymbolNotFoundError(Location location, string syntaxName) =>
            Create($"{ErrorId}001", "Symbol not found.", $"Unable to find any symbols for {syntaxName}", location);

        internal static Diagnostic MapFromAttributeNotFoundError(Location location) =>
            Create($"{ErrorId}002", "Attribute Not Available", $"Unable to find {MapFromAttributeSource.AttributeName} type.", location);

        internal static Diagnostic NoMatchingPropertyFoundError(Location location, string className, string sourceTypeName) =>
            Create($"{ErrorId}003", "Property Not Found", $"No matching properties found between '{className}' and '{sourceTypeName}' types.", location);

        internal static Diagnostic ConfigurationParseError(string error) =>
            Create($"{ErrorId}004", "Incorrect Configuration", error, Location.None);
     
        private static Diagnostic Create(string id, string title, string message, Location location, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
            Diagnostic.Create(new DiagnosticDescriptor(id, title, message, UsageCategory, severity, true), location);
    }
}