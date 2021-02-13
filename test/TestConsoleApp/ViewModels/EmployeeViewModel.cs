using MapTo;
using TestConsoleApp.Data.Models;

namespace TestConsoleApp.ViewModels
{
    [MapFrom(typeof(Employee))]
    public partial class EmployeeViewModel
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public ManagerViewModel Manager { get; set; }
    }
}
