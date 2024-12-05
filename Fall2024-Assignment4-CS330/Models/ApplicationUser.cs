﻿using Microsoft.AspNetCore.Identity;

namespace Fall2024_Assignment4_CS330.Models
{
    public class ApplicationUser : IdentityUser
    { 
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public string UserType { get; set; }
        public byte[]? Photo { get; set; }
    }
}
