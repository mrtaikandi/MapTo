using System.Collections.Generic;
using MapTo.Integration.Tests.Data.Models;

namespace MapTo.Integration.Tests.Data.ViewModels
{
    [MapFrom(typeof(Manager))]
    public partial class ManagerViewModel : EmployeeViewModel
    {
        public int Level { get; set; }

        public List<EmployeeViewModel> Employees { get; set;  } = new();
    }
}