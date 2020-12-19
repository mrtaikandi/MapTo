using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MapTo.Models
{
    public class MapModel
    {
        public MapModel(string? ns, string classModifiers, string className, IEnumerable<IPropertySymbol> properties, string destinationNamespace, string destinationClassName, IEnumerable<IPropertySymbol> destinationTypeProperties)
        {
            Namespace = ns;
            ClassModifiers = classModifiers;
            ClassName = className;
            Properties = properties;
            DestinationNamespace = destinationNamespace;
            DestinationClassName = destinationClassName;
            DestinationTypeProperties = destinationTypeProperties;
        }

        public string? Namespace { get; }

        public string ClassModifiers { get; }

        public string ClassName { get;  }
        
        public IEnumerable<IPropertySymbol> Properties { get; }
        
        public string DestinationNamespace { get; }
        
        public string DestinationClassName { get; }
        
        public IEnumerable<IPropertySymbol> DestinationTypeProperties { get; }

        public bool IsEmpty => !Properties.Any() || !DestinationTypeProperties.Any();
    }
}