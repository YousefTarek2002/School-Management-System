using System.ComponentModel.DataAnnotations;

namespace School.Models.VM
{
    public class TeacherVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string Email { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
    }


    

}
