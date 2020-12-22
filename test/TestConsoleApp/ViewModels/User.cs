// using MapTo;

using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Data.Models.User))]
    public partial class User
    {
        public string FirstName { get; }
    }
}