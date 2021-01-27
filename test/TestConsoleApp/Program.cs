using TestConsoleApp.ViewModels;

namespace TestConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var userViewModel = User.From(new Data.Models.User());
            var userViewModel2 = new Data.Models.User().ToUserViewModel();
        }
    }
}