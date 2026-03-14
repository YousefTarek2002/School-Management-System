namespace SchoolSystem.Models.VM
{
    public class StudentDetailsVM
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public List<string> Classes { get; set; } = new();
        public int AttendanceCount { get; set; }
        public int ExamResultsCount { get; set; }
    }
    // StudentDashboardVM.cs
    public class StudentDashboardVM
    {
        public Student? Student { get; set; }
        public IEnumerable<Fee> Fees { get; set; } = new List<Fee>();
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public string StudentClass { get; set; } = "";
    }

}
