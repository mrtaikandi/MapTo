using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MapTo.Models
{
    public class MapModel
    {
        public MapModel(string? ns, SyntaxTokenList classModifiers, string className, IEnumerable<IPropertySymbol> properties, string sourceNamespace, string sourceClassName, IEnumerable<IPropertySymbol> sourceTypeProperties)
        {
            Namespace = ns;
            ClassModifiers = classModifiers;
            ClassName = className;
            Properties = properties;
            SourceNamespace = sourceNamespace;
            SourceClassName = sourceClassName;
            SourceTypeProperties = sourceTypeProperties;
        }

        public string? Namespace { get; }

        public SyntaxTokenList ClassModifiers { get; }

        public string ClassName { get;  }
        
        public IEnumerable<IPropertySymbol> Properties { get; }
        
        public string SourceNamespace { get; }
        
        public string SourceClassName { get; }
        
        public IEnumerable<IPropertySymbol> SourceTypeProperties { get; }
    }
}