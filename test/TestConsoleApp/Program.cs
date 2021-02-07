using System;
using TestConsoleApp.Data.Models;
using TestConsoleApp.ViewModels;

namespace TestConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var user = new User
            {
                Id = 1234,
                RegisteredAt = DateTimeOffset.Now,
                Profile = new Profile
                {
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            var vm = user.ToUserViewModel();

            Console.WriteLine("Key: {0}", vm.Key);
            Console.WriteLine("RegisteredAt: {0}", vm.RegisteredAt);
            Console.WriteLine("FirstName: {0}", vm.Profile.FirstName);
            Console.WriteLine("LastName: {0}", vm.Profile.LastName);
        }
    }
}