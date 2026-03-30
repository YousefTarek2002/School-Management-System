namespace School.Models
{
    public class Fee
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public bool Paid { get; set; }
        public DateTime DueDate { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }

}
