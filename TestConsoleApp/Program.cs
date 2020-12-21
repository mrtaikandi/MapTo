using System;
using TestConsoleApp.ViewModels;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var userViewModel = User.From(new Data.Models.User());
            var userViewModel2 = UserViewModel.From(new Data.Models.User());
            
        }
    }
}