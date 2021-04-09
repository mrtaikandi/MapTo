namespace MapTo.Integration.Tests.Data.Models
{
    public class Employee
    {
        private Manager _manager;

        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public Manager Manager
        {
            get => _manager;
            set
            {
                if (value == null)
                {
                    _manager.Employees.Remove(this);
                }
                else
                {
                    value.Employees.Add(this);
                }

                _manager = value;
            }
        }
    }
}