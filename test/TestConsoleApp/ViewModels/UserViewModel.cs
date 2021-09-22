using System;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(User))]
    [MapFrom(typeof(UserDto))]
    public partial class UserViewModel
    {
        [MapProperty(SourcePropertyName = nameof(User.Id), SourceTypeName = typeof(User))]
        [MapProperty(SourcePropertyName = nameof(UserDto.Id), SourceTypeName = typeof(UserDto))]
        [MapTypeConverter(typeof(IdConverter), SourceTypeName = typeof(UserDto))]
        [MapTypeConverter(typeof(IdConverter), SourceTypeName = typeof(User))]
        public string Key { get; }
        public DateTimeOffset RegisteredAt { get; set; }

        //[MapProperty(SourcePropertyName = nameof(UserDto.Name))]
        [MapProperty(SourcePropertyName = nameof(UserDto.Name), SourceTypeName = typeof(UserDto))]
        public string TestName { get; set; }
        
        //[IgnoreProperty(SourceTypeName = typeof(User))]
        public ProfileViewModel Profile { get; set; }

        private class IdConverter : ITypeConverter<int, string>
        {
            public string Convert(int source, object[]? converterParameters) => $"{source:X}";
        }
    }
}