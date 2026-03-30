namespace School.Models.VM
{
    public class ExamVM
    {
        public int Id { get; set; }

        [Required]
        public string ExamName { get; set; } = "";

        [Required]
        public int SubjectId { get; set; }
        public IEnumerable<SelectListItem>? Subjects { get; set; }

        [Required]
        public int ClassId { get; set; }
        public IEnumerable<SelectListItem>? Classes { get; set; }

        [Required]
        public DateTime ExamDate { get; set; } = DateTime.Today;

        [Required]
        public TimeSpan ExamTime { get; set; } = TimeSpan.FromHours(9);

        public IFormFile? TimeTableFile { get; set; }
        public string? ExistingTimeTablePath { get; set; }
        public string? SubjectName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public int StudentsCount { get; set; }
    }

    public class ExamResultIndexVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int TotalStudents { get; set; }
        public bool HasResults { get; set; }
    }
    public class ExamResultVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public List<StudentResultVM> Students { get; set; } = new();
    }

    public class StudentResultVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        [Range(0, 100)]
        public decimal Grade { get; set; }
        public decimal CurrentGrade { get; set; }
    }
    public class ExamResultDisplayVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public decimal Grade { get; set; }
        public int Rank { get; set; }
    }

    public class ExamResultsViewVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int TotalStudents { get; set; }
        public decimal HighestGrade { get; set; }
        public decimal AverageGrade { get; set; }
        public List<ExamResultDisplayVM> Results { get; set; } = new();
    }
    namespace School.Models.VM
    {
        public class ExamDetailsVM
        {
            public int Id { get; set; }
            public string ExamName { get; set; } = "";
            public string SubjectName { get; set; } = "";
            public string ClassName { get; set; } = "";
            public DateTime ExamDate { get; set; }
            public TimeSpan ExamTime { get; set; }
            public string? TimeTablePath { get; set; }

            public List<ExamStudentVM> Students { get; set; } = new();
        }

        public class ExamStudentVM
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string Email { get; set; } = "";
            public decimal? Grade { get; set; }

        }
    }
}
