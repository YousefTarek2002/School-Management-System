namespace School.Models.VM
{
    public class FeeVM
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public IEnumerable<SelectListItem>? Students { get; set; }

        [Required]
        [Range(1, 100000)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public bool Paid { get; set; }

        // Display properties
        public string? StudentName { get; set; }
        public string? ClassName { get; set; }
        public string? PaymentStatus { get; set; }
        public int DaysOverdue { get; set; }
    }
    public class StudentFeesVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public List<FeeVM> Fees { get; set; } = new();
    }
}