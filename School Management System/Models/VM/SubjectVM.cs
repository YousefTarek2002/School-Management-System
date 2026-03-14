using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models.VM
{
    public class SubjectVM
    {
        public int Id { get; set; }

        [Required]
     
        public string? Name { get; set; }

        public List<string> TeacherNames { get; set; } = new List<string>();
        public List<int>? SelectedTeachers { get; set; }
        public List<Teacher> AllTeachers { get; set; } = new List<Teacher>();
    }
}
