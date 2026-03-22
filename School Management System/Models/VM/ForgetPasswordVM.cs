
namespace School.Models.VM
{
    public class ForgetPasswordVM
    {
        public int Id { get; set; }

        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}
