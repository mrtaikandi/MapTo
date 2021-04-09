using System.Linq;
using MapTo.Integration.Tests.Data.Models;
using MapTo.Integration.Tests.Data.ViewModels;
using Shouldly;
using Xunit;

namespace MapTo.Integration.Tests
{
    public class CyclicReferenceTests
    {
        [Fact]
        public void VerifySelfReference()
        {
            // Arrange
            var manager = new Manager { Id = 1, EmployeeCode = "M001", Level = 100 };
            manager.Manager = manager;
            
            // Act
            var result = manager.ToManagerViewModel();
            
            // Assert
            result.Id.ShouldBe(manager.Id);
            result.EmployeeCode.ShouldBe(manager.EmployeeCode);
            result.Level.ShouldBe(manager.Level);
            result.Manager.ShouldBeSameAs(result);
        }

        [Fact]
        public void VerifyNestedReference()
        {
            // Arrange
            var manager1 = new Manager { Id = 100, EmployeeCode = "M001", Level = 100 };
            var manager2 = new Manager { Id = 102, EmployeeCode = "M002", Level = 100 };
            
            var employee1 = new Employee { Id = 200, EmployeeCode = "E001"};
            var employee2 = new Employee { Id = 201, EmployeeCode = "E002"};
            
            employee1.Manager = manager1;
            employee2.Manager = manager2;

            manager2.Manager = manager1;
            
            // Act
            var manager1ViewModel = manager1.ToManagerViewModel();
            
            // Assert
            manager1ViewModel.Id.ShouldBe(manager1.Id);
            manager1ViewModel.Manager.ShouldBeNull();
            manager1ViewModel.Employees.Count.ShouldBe(2);
            manager1ViewModel.Employees[0].Id.ShouldBe(employee1.Id);
            manager1ViewModel.Employees[0].Manager.ShouldBeSameAs(manager1ViewModel);
            manager1ViewModel.Employees[1].Id.ShouldBe(manager2.Id);
            manager1ViewModel.Employees[1].Manager.ShouldBeSameAs(manager1ViewModel);
        }
        
        [Fact]
        public void VerifyNestedSelfReference()
        {
            // Arrange
            var manager1 = new Manager { Id = 100, EmployeeCode = "M001", Level = 100 };
            var manager3 = new Manager { Id = 101, EmployeeCode = "M003", Level = 100 };
            var manager2 = new Manager { Id = 102, EmployeeCode = "M002", Level = 100 };
            
            var employee1 = new Employee { Id = 200, EmployeeCode = "E001"};
            var employee2 = new Employee { Id = 201, EmployeeCode = "E002"};
            var employee3 = new Employee { Id = 202, EmployeeCode = "E003"};
            
            employee1.Manager = manager1;
            employee2.Manager = manager2;
            employee3.Manager = manager3;

            manager2.Manager = manager1;
            manager3.Manager = manager2;
            
            // Act
            var manager3ViewModel = manager3.ToManagerViewModel();
            
            // Assert
            manager3ViewModel.Manager.ShouldNotBeNull();
            manager3ViewModel.Manager.Id.ShouldBe(manager2.Id);
            manager3ViewModel.Manager.Manager.Id.ShouldBe(manager1.Id);
            manager3ViewModel.Employees.All(e => ReferenceEquals(e.Manager, manager3ViewModel)).ShouldBeTrue();
        }
    }
}