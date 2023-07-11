namespace MapTo.Tests.SourceBuilders;

[Flags]
internal enum PropertyType
{
    AutoProperty = 1,
    PropertyWithBackingField = 2,
    ReadOnly = 4,
    InitProperty = 8
}