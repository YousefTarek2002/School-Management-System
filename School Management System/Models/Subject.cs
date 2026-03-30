namespace School.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
        public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = new List<SubjectTeacher>();
    }
}
