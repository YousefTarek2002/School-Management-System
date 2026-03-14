using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace School.Models.VM
{

    public class AttendanceVM
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public IEnumerable<SelectListItem>? Students { get; set; }

        [Required]
        public int ClassId { get; set; }
        public IEnumerable<SelectListItem>? Classes { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public bool Status { get; set; }   

        // Display properties
        public string? StudentName { get; set; }
        public string? ClassName { get; set; }
        public string? StatusText { get; set; }   
    }
    public class BulkAttendanceVM
    {
        public int ClassId { get; set; }
        public DateTime Date { get; set; }
        public List<BulkStudentAttendanceVM> Students { get; set; } = new();
    }

    public class BulkStudentAttendanceVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public bool IsPresent { get; set; }
    }
}