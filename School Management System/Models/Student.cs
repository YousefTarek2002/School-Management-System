using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateTime BD { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Phone { get; set; } = string.Empty;

        public ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<BookIssue> BookIssues { get; set; } = new List<BookIssue>();
        public ICollection<Fee> Fees { get; set; } = new List<Fee>();
    }
}
