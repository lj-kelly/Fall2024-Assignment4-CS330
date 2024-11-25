using System;
using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Collections.Specialized;
using Fall2024_Assignment4_CS330.Services;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class TTTController : Controller
    {
        private static TTTModel game = new TTTModel(); // Simulating a session-level game instance
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OpenAIService _openAIService;

        public TTTController(UserManager<ApplicationUser> userManager, OpenAIService openAIService)
        {
            _userManager = userManager;
            _openAIService = openAIService;
        }

        // GET: TTT/Index
        public ActionResult Index()
        {
            return View(game);
        }

        // GET: TTT/Local
        public ActionResult Local()
        {
            game = new TTTModel();
            game.Mode = "Local";
            return View("Index", game);
        }

        // GET: TTT/Online
        public ActionResult Online()
        {
            game = new TTTModel();
            game.Mode = "Online";
            return View("Index", game);
        }

        // GET: TTT/ChatGPT
        public ActionResult ChatGPT()
        {
            game = new TTTModel();
            game.Mode = "ChatGPT";
            return View("Index", game);
        }

        // POST: TTT/MakeMove
        [HttpPost]
        public async Task<ActionResult> MakeMove(int row, int col)
        {
            if (game.IsCellEmpty(row, col))
            {
                // Handle the move based on the game mode
                switch (game.Mode)
                {
                    case "Local":
                        MakeLocalMove(row, col);
                        break;
                    case "Online":
                        await MakeOnlineMove(row, col);
                        break;
                    case "ChatGPT":
                        await MakeChatGPTMove(row, col);
                        break;
                }
            }

            // Check for winner or draw
            char winner = game.CheckWinner();
            if (winner != '\0')
            {
                ViewBag.Message = $"Player {winner} wins!";
                await IncrementWins();
            }
            else if (game.IsDraw())
            {
                ViewBag.Message = "It's a draw!";
            }

            return View("Index", game);
        }

        // Local Game Move Logic
        private void MakeLocalMove(int row, int col)
        {
            // Alternate turns between 'X' and 'O'
            game.MakeMove(row, col);
        }

        // Online Game Move Logic
        private async Task MakeOnlineMove(int row, int col)
        {
            // Determine the current player based on their logged-in status
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            char currentPlayer = userId == game.Player1Id ? 'X' : 'O';

            if (game.CurrentPlayer == currentPlayer && game.IsCellEmpty(row, col))
            {
                game.MakeMove(row, col);
                game.TogglePlayer(); // Switch turns
            }
        }

        // ChatGPT Game Move Logic
        private async Task MakeChatGPTMove(int row, int col)
        {
            // Player makes a move
            game.MakeMove(row, col);

            // Check if player won
            if (game.CheckWinner() == '\0' && !game.IsDraw())
            {
                // ChatGPT (AI) makes its move if the game isn't over
                List<int> gptMove = await _openAIService.GetNextMove(game);
                game.MakeMove(gptMove[0], gptMove[1]);
            }
        }

        // GET: TTT/Reset
        public ActionResult Reset()
        {
            // Wipe the board but keep the current game mode and player details
            game.BoardString = new string('\0', 9); // Set all cells to empty
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
