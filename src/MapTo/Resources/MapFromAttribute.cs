// <auto-generated />

namespace MapTo
{
    using System;
    
    /// <summary>
    /// Specifies the source type to map from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MapFromAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapFromAttribute"/> class.
        /// </summary>
        /// <param name="sourceType">The source type to map from.</param>
        public MapFromAttribute(Type sourceType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));    
            }
            
            SourceType = sourceType;
        }

        /// <summary>
        /// Gets the source type to map from.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER 
        [System.Diagnostics.CodeAnalysis.NotNull]
#endif
        public Type SourceType { get; }
    }
}