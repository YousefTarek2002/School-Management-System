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
    public class EditStudentVM
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        public DateTime BD { get; set; }

        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        public int? ClassId { get; set; }
    }
}
