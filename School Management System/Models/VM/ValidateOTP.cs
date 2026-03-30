using System.ComponentModel.DataAnnotations;

namespace School.Models.VM
{
    public class ValidateOTP
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        public string OTP { get; set; } = string.Empty;
    }
}