using System;
using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class TTTController : Controller
    {
        private static TTTModel game = new TTTModel(); // Simulating a session-level game instance
        private readonly UserManager<ApplicationUser> _userManager;

        public TTTController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: TTT
        public ActionResult Index()
        {
            return View(game);
        }

        // POST: TTT/MakeMove
        [HttpPost]
        public async Task<ActionResult> MakeMove(int row, int col)
        {
            if (game.IsCellEmpty(row, col))
            {
                game.MakeMove(row, col);
            }

            if (game.CheckWinner() != '\0')
            {
                // Increment wins for the logged-in user
                await IncrementWins();

                ViewBag.Message = $"Player {game.CheckWinner()} wins!";
            }
            else if (game.IsDraw())
            {
                ViewBag.Message = "It's a draw!";
            }

            return View("Index", game);
        }

        // GET: TTT/Reset
        public ActionResult Reset()
        {
            game = new TTTModel(); // Resetting the game
            return RedirectToAction("Index");
        }

        // Method to increment wins for the logged-in user
        private async Task IncrementWins()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                user.GamesWon++;
                await _userManager.UpdateAsync(user);
            }
        }
    }
}
