

namespace School.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        public string? ExamName { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Required]
        public int ClassId { get; set; }
        public Class? Class { get; set; }

        [Required]
        public DateTime ExamDate { get; set; } = DateTime.Now;

        [Required]
        public TimeSpan ExamTime { get; set; }

        public string? TimeTablePath { get; set; }

        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}
