using System;
using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Collections.Generic;
using Fall2024_Assignment4_CS330.Services;
using Azure;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class TTTController : Controller
    {
        private static TTTModel game = new TTTModel(); // Simulating a session-level game instance
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OpenAIService _openAIService;

        // Tracking restricted grids for each player
        private static int? restrictedGridX = null; // The grid player X is restricted to
        private static int? restrictedGridO = null; // The grid player O is restricted to

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
        public async Task<ActionResult> MakeMove(int gridRow, int gridCol, int cellRow, int cellCol)
        {
            // Determine which player is making the move
            char currentPlayer = game.CurrentPlayer;

            // Check if the player is restricted and ensure they are moving in the correct grid
            if ((currentPlayer == 'X' && restrictedGridX.HasValue && restrictedGridX != GetGridIndex(gridRow, gridCol)) ||
                (currentPlayer == 'O' && restrictedGridO.HasValue && restrictedGridO != GetGridIndex(gridRow, gridCol)))
            {
                ViewBag.Message = "You are restricted to the highlighted grid.";
                return View("Index", game);
            }

            // If the move is valid, make the move
            if (game.Board[gridRow, gridCol, cellRow, cellCol] == '\0')
            {
                game.MakeMove(gridRow, gridCol, cellRow, cellCol);
                int nextGrid = GetGridIndex(cellRow, cellCol);
                if (!IsGridAvailable(nextGrid))
                {
                    nextGrid = GetRandomAvailableGrid();
                    if (nextGrid == -1)
                    {
                        ViewBag.Message = "It's a draw!";
                        game.GameWinner = 'T';
                        await IncrementWins(true);
                        return View("Index", game);
                    }
                }
                game.RestrictedGrid = nextGrid;

                if (currentPlayer == 'X')
                {
                    restrictedGridO = nextGrid;
                }
                else
                {
                    restrictedGridX = nextGrid;
                }

                // Check for winners in the grid
                char gridWinner = game.CheckGridWinner(gridRow, gridCol);
                if (gridWinner != '\0')
                {
                    ViewBag.Message = $"Grid {GetGridIndex(gridRow, gridCol) + 1} won by Player {gridWinner}!";
                }

                // Check for winner on the whole board
                char boardWinner = game.CheckBoardWinner();
                if (boardWinner != '\0')
                {
                    ViewBag.Message = $"Player {boardWinner} wins the game!";
                    game.GameWinner = boardWinner;
                    await IncrementWins(false);
                }
                else if (!IsBoardAvailable())
                {
                    ViewBag.Message = "It's a draw!";
                }
            }
            else
            {
                ViewBag.Message = "Invalid move. Cell already occupied.";
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


        // POST: TTT/GetHint
        [HttpPost]
        public async Task<IActionResult> GetHint()
        {
            try
            {
                Console.WriteLine("Greetings");
                string response = await _openAIService.GetHint(game);
                Console.WriteLine(response);
                ViewBag.Hint = response;

                return View("Index", game);
            }

            catch (Exception ex)
            {
                ViewBag.Hint = "Sorry, but we couldn't fetch your hint.";
                return View("Index", game);

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
        private async Task IncrementWins(bool tie)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                user.GamesWon++;
                await _userManager.UpdateAsync(user);
            }
        }

        // Helper method to convert grid position to index
        private int GetGridIndex(int outerRow, int outerCol)
        {
            return outerRow * 3 + outerCol;
        }

        private bool IsGridAvailable(int gridIndex)
        {
            int gridRow = gridIndex / 3;
            int gridCol = gridIndex % 3;
            if (game.CheckGridWinner(gridRow, gridCol) != '\0') return false;
            // no winner and topleft square empty
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (game.Board[gridRow, gridCol, i, j] == '\0') return true;
                }
            }
            return false;
        }

        private bool IsBoardAvailable()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (IsGridAvailable(GetGridIndex(i, j))) return true;
                }
            }
            return false;
        }

        // Method to get a random available grid
        private int GetRandomAvailableGrid()
        {
            List<int> availableGrids = new List<int>();

            for (int i = 0; i < 9; i++)
            {
                if (IsGridAvailable(i))
                {
                    availableGrids.Add(i);
                }
            }

            if (availableGrids.Count == 0) return -1;

            Random rand = new Random();
            return availableGrids[rand.Next(availableGrids.Count)];
        }
    }
}
