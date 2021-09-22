using System;
using MapTo;
using TestConsoleApp.Data.Models;
using TestConsoleApp.ViewModels;
using TestConsoleApp.ViewModels2;

namespace TestConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            UserTest();
            CyclicReferenceTest();
             
            // EmployeeManagerTest();
            Console.WriteLine("done");
        }

        private static void EmployeeManagerTest()
        {
            var manager1 = new Manager
            {
                Id = 1,
                EmployeeCode = "M001",
                Level = 100
            };

            var manager2 = new Manager
            {
                Id = 2,
                EmployeeCode = "M002",
                Level = 100,
                Manager = manager1
            };

            var employee1 = new Employee
            {
                Id = 101,
                EmployeeCode = "E101",
                Manager = manager1
            };

            var employee2 = new Employee
            {
                Id = 102,
                EmployeeCode = "E102",
                Manager = manager2
            };

            manager1.Employees = new[] { employee1, manager2 };
            manager2.Employees = new[] { employee2 };

            manager1.ToManagerViewModel();
            employee1.ToEmployeeViewModel();
        }

        private static ManagerViewModel CyclicReferenceTest()
        {
            var manager1 = new Manager
            {
                Id = 1,
                EmployeeCode = "M001",
                Level = 100
            };

            manager1.Manager = manager1;
            return manager1.ToManagerViewModel();
        }

        private static void UserTest()
        {
            var user = new User
            {
                Id = 1234,
                RegisteredAt = DateTimeOffset.Now,
                Profile = new Profile
                {
                    FirstName = "John",
                    LastName = "Doe"
                },
                Name = "UserName"
            };

            var userDto = new UserDto
            {
                Id = 123,
                Name = "UserDtoName"
            };
            var vm = user.ToUserViewModel();

            var vm2 = userDto.ToUserViewModel();


            Console.WriteLine("Key: {0}", vm.Key);
            Console.WriteLine("RegisteredAt: {0}", vm.RegisteredAt);
            Console.WriteLine("FirstName: {0}", vm.Profile.FirstName);
            Console.WriteLine("LastName: {0}", vm.Profile.LastName);
            Console.WriteLine("TestName: {0}", vm.TestName);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Key: {0}", vm2.Key);
            Console.WriteLine("RegisteredAt: {0}", vm2.RegisteredAt);
            Console.WriteLine("TestName: {0}", vm2.TestName);
        }
    }
}