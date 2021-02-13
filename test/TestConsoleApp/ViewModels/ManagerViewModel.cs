using System;
using System.Collections.Generic;
using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Manager))]
    public partial class ManagerViewModel : EmployeeViewModel
    {
        public int Level { get; set; }

        public IEnumerable<EmployeeViewModel> Employees { get; set; } = Array.Empty<EmployeeViewModel>();
    }
}