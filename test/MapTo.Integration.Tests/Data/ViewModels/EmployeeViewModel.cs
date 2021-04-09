using MapTo.Integration.Tests.Data.Models;

namespace MapTo.Integration.Tests.Data.ViewModels
{
    [MapFrom(typeof(Employee))]
    public partial class EmployeeViewModel
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public ManagerViewModel Manager { get; set; }
    }
}