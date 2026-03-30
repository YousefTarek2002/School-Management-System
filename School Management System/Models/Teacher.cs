using System.ComponentModel.DataAnnotations.Schema;

namespace School.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        public string? ConfirmPassword { get; set; }

        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = new List<SubjectTeacher>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}
