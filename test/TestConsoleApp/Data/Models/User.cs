using System;

namespace TestConsoleApp.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public DateTimeOffset RegisteredAt { get; set; }

        public Profile Profile { get; set; }
    }
}