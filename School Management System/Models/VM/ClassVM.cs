
namespace SchoolSystem.Models.VM
{
    public class ClassVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Section { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public int StudentsCount { get; set; }
        public int TotalStudents { get; set; }
    }
    public class AddClassVM
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Section { get; set; } = "";

        [Required]
        public int TeacherId { get; set; }
    }
}
