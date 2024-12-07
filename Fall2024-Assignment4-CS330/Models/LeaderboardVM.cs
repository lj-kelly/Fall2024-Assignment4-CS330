using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace Fall2024_Assignment4_CS330.Models
{
    public class LeaderboardVM
    {
        private List<ApplicationUser> _users;

        public List<ApplicationUser> Users
        {
            get => _users.OrderByDescending(u => u.WinRate).ToList(); // Sort by W/L ratio
            set => _users = value;
        }
        public async Task<LeaderboardVM> GetLeaderboardAsync(UserManager<ApplicationUser> userManager)
        {
            var users = userManager.Users.ToList(); // Retrieve all users
            var leaderboard = new LeaderboardVM
            {
                Users = users // Sorting happens automatically in the property
            };
            return leaderboard;
        }

    }
}
