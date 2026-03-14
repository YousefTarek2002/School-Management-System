
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace School.Models
{
    public class Class
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public int TeacherId { get; set; }

        [ValidateNever]
        public Teacher Teacher { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }

}

