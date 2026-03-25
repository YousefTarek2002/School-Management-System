using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models.VM
{
    public class BookVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان طويل جداً")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "المؤلف مطلوب")]
        [StringLength(150)]
        public string Author { get; set; } = "";

        [Required(ErrorMessage = "النسخ الكلية مطلوبة")]
        [Range(1, 1000, ErrorMessage = "النسخ بين 1 و 1000")]
        public int TotalCopies { get; set; }

        [Required(ErrorMessage = "النسخ المتاحة مطلوبة")]
        [Range(0, 1000, ErrorMessage = "النسخ المتاحة بين 0 و 1000")]
        public int CopiesAvailable { get; set; }

        [StringLength(20)]
        public string? ISBN { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        // Index properties
        public int IssuedCount { get; set; }
        public List<string>? ActiveIssues { get; set; } = new();
    }

    public class IssueBookVM
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "يجب اختيار طالب")]
        [Range(1, int.MaxValue, ErrorMessage = "اختر طالب صحيح")]
        public int StudentId { get; set; }
    }
    public class BookDetailsVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }

        public int TotalCopies { get; set; }
        public int CopiesAvailable { get; set; }

        public List<BookIssueVM> Issues { get; set; } = new();
    }

    public class BookIssueVM
    {
        public string StudentName { get; set; }
        public string Email { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime ReturnDate { get; set; }

        public bool IsReturned => ReturnDate < DateTime.Now;
    }

}
