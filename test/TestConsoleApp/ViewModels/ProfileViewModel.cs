using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Profile))]
    public partial class ProfileViewModel
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}