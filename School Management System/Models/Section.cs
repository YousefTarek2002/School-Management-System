namespace School.Models
{

    public class Section
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }

}
