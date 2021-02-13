using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsoleApp.Data.Models
{
    public class Manager: Employee
    {
        public int Level { get; set; }

        public IEnumerable<Employee> Employees { get; set; } = Array.Empty<Employee>();
    }
}
