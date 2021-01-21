using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Data.Models.User))]
    public partial class UserViewModel
    {
        public string FirstName { get; }

        [IgnoreProperty]
        public string LastName { get; }
        
        [MapProperty(Converter = typeof(LastNameConverter))]
        public string Key { get; }

        private class LastNameConverter : ITypeConverter<long, string>
        {
            /// <inheritdoc />
            public string Convert(long source) => $"{source} :: With Type Converter";
        }
    }
}