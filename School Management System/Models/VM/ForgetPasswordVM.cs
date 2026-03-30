using System.ComponentModel.DataAnnotations;

namespace School.Models.VM
{
    public class ForgetPasswordVM
    {
        [Required(ErrorMessage = "Username or Email is required")]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}