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
    public class EditTeacherVM
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public decimal Salary { get; set; }

        [Required]
        public string? Email { get; set; }

        public int? SubjectId { get; set; }
    }
    public class TeacherDetailsVM
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Salary { get; set; }
        public string? Email { get; set; }

        public List<string> Subjects { get; set; } = new();
    }


}
