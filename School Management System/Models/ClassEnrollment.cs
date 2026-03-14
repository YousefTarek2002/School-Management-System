namespace School.Models
{

    public class ClassEnrollment
    {
        public int Id { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public bool Status { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; }
    }
}
