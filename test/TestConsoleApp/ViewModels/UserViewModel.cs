using System;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(User))]
    public partial class UserViewModel
    {
        [MapProperty(SourcePropertyName = nameof(User.Id))]
        [MapTypeConverter(typeof(IdConverter))]
        public string Key { get; }

        public DateTimeOffset RegisteredAt { get; set; }
        
        // [IgnoreProperty]
        public ProfileViewModel Profile { get; set; }

        private class IdConverter : ITypeConverter<int, string>
        {
            public string Convert(int source, object[] converterParameters) => $"{source:X}";
        }
    }
}