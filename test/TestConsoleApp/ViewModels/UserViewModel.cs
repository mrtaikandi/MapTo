using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Data.Models.User))]
    public partial class UserViewModel
    {
        public string FirstName { get; }

        [IgnoreProperty]
        public string LastName { get; }
        
        [MapProperty(converter: typeof(LastNameConverter))]
        public string Key { get; }

        private class LastNameConverter : ITypeConverter<int, string>
        {
            /// <inheritdoc />
            public string Convert(int source) => $"{source} :: With Type Converter";
        }
    }
}