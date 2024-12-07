using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class LeaderboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaderboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Leaderboard/Index
        public async Task<IActionResult> Index()
        {
            // Fetch all users
            var users = _userManager.Users.ToList();

            // Populate the leaderboard view model and sort by W/L ratio
            var leaderboardVM = new LeaderboardVM
            {
                Users = users.OrderByDescending(u =>
                    u.GamesLost == 0 && u.GamesWon > 0 ? u.GamesWon : // Avoid divide by zero
                    (u.GamesLost == 0 && u.GamesWon == 0 ? 0 : (double)u.GamesWon / u.GamesLost))
                    .ThenByDescending(u => u.GamesWon) // Secondary sorting for ties
                    .ToList()
            };

            // Pass the model to the view
            return View(leaderboardVM);
        }
    }
}
