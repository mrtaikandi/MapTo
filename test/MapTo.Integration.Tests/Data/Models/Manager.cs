using System.Collections.Generic;

namespace MapTo.Integration.Tests.Data.Models
{
    public class Manager : Employee
    {
        public int Level { get; set; }

        public List<Employee> Employees { get; set; } = new();
    }
}