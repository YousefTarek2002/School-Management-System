namespace School.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }

        public int TotalCopies { get; set; }
        public int CopiesAvailable { get; set; }

        public ICollection<BookIssue> BookIssues { get; set; } = new List<BookIssue>();
    }
}
