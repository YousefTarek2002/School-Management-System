namespace School.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public decimal Grade { get; set; }
    }

}
