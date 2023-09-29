namespace MapTo.CodeAnalysis;

internal static class SourceProductionContextExtensions
{
    internal static void ReportDiagnostics(this SourceProductionContext context, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}