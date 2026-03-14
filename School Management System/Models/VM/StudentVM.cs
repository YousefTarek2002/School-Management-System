using System.ComponentModel.DataAnnotations;

namespace School.Models.VM
{
    public class StudentVM
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ClassName { get; set; } = "";
    }
}
