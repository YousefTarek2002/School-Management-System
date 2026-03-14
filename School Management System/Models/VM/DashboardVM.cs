namespace SchoolSystem.Models.VM
{
    public class DashboardVM
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalExams { get; set; }
        public decimal TotalFeesDue { get; set; }
        public decimal TotalFeesPaid { get; set; }
        public List<RecentItemVM> RecentStudents { get; set; } = new();
        public List<RecentItemVM> RecentFees { get; set; } = new();
    }

    public class RecentItemVM
    {
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
    }

}
