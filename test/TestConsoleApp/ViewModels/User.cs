// using MapTo;

using MapTo;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Data.Models.User))]
    public partial class User
    {
        public int Id { get; }
        
        public string FirstName { get; }
        
        public string LastName { get; }
    }
}