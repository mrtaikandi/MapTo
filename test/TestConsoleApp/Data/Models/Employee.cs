using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsoleApp.Data.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public Manager Manager { get; set; }
    }
}
