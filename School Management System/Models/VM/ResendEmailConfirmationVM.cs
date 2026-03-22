namespace School.Models.VM
{
    public class ResendEmailConfirmationVM
    {
        public int Id { get; set; }

        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
    }
}
