
// using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapTo.MapFrom(typeof(Data.Models.User))]
    public partial class User
    {
        public string FirstName { get; }
    }
}