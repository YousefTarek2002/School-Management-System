using School.Models;
using System.ComponentModel.DataAnnotations;

namespace School.Models
{
    public class ApplicationUserOTP
    {
        [Key]
        public string Id { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public string OTP { get; set; }
        public DateTime CreateAt { get; set; }
        public bool IsValid { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
