using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment4_CS330.Models
{
    public class RegisterVM
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public byte[]? Photo { get; set; }
    }
}
