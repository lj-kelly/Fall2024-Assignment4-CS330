using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment4_CS330.Models
{
    public class ManageAccountVM
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }

        [EmailAddress]
        public string Email { get; set; } // Include any other properties you want to manage
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int GamesPlayed { get; set; }
    }
}
