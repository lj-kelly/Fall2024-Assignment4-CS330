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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Timers;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class TTTController : Controller
    {
        private static TTTModel game = new TTTModel(); // Simulating a session-level game instance
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OpenAIService _openAIService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private GameTimerService _gameTimerService;
        private string _userType; //set the usertype as global variable
        private System.Timers.Timer _timer;
        // Tracking restricted grids for each player
        private static int? restrictedGridX = null; // The grid player X is restricted to
        private static int? restrictedGridO = null; // The grid player O is restricted to

        public TTTController(UserManager<ApplicationUser> userManager, OpenAIService openAIService, IHttpContextAccessor httpContextAccessor, GameTimerService gameTimerService)
        {
            _userManager = userManager;
            _openAIService = openAIService;
            _httpContextAccessor = httpContextAccessor;          
            _gameTimerService = gameTimerService;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _userType = User.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value ?? "Standard";
            if (game.Mode == Mode.ChatGPT) _userType = "Standard";
            base.OnActionExecuting(context);
        }

        // GET: TTT/Index
        public ActionResult Index()
        {
            return View(game);
        }

        // GET: TTT/Local
        public ActionResult Local(string localTimeLimit)
        {
            game = new TTTModel();
            restrictedGridX = null;
            restrictedGridO = null;
            game.Mode = Mode.Local;
            game.Status = Status.Active;
            game.MaxTime = int.Parse(localTimeLimit) * 60;
            game.Player1Time = game.MaxTime;
            game.Player2Time = game.MaxTime;

            _gameTimerService.AddGame(game);
            return View(_userType == "Standard" ? "Standard" : "Pro", game);
        }
            
        public ActionResult ChatGPT(string chatgptTimeLimit)
        {
            game = new TTTModel();
            restrictedGridX = null;
            restrictedGridO = null;
            game.Mode = Mode.ChatGPT;
            game.Status = Status.Active;
            game.MaxTime = int.Parse(chatgptTimeLimit) * 60;
            game.Player1Time = game.MaxTime;
            game.Player2Time = game.MaxTime;

            _gameTimerService.AddGame(game);
            return View("Standard", game);
        }

        // POST: TTT/MakeMove
        [HttpPost]
        public async Task<ActionResult> MakeMove(int gridRow, int gridCol, int cellRow, int cellCol)
        {
            if (game.GameWinner == 'X')
            {
                ViewBag.Message = $"Player X wins the game!";
                game.Status = Status.Complete;
                if (game.Mode == Mode.ChatGPT)
                {
                    //await IncrementWins();
                }
                return View(_userType == "Standard" ? "Standard" : "Pro", game);
            }
            else if (game.GameWinner == 'O')
            {
                ViewBag.Message = $"Player O wins the game!";
                game.Status = Status.Complete;
                if (game.Mode == Mode.ChatGPT)
                {
                    //await IncrementLosses();
                }
                return View(_userType == "Standard" ? "Standard" : "Pro", game);
            }

            Console.WriteLine($"Player 1 Time: {game.Player1Time}s, Player 2 Time: {game.Player2Time}s");
            // Determine which player is making the move
            char currentPlayer = game.CurrentPlayer;

            // Check if the player is restricted and ensure they are moving in the correct grid
            if ((currentPlayer == 'X' && restrictedGridX.HasValue && restrictedGridX != GetGridIndex(gridRow, gridCol)) ||
                (currentPlayer == 'O' && restrictedGridO.HasValue && restrictedGridO != GetGridIndex(gridRow, gridCol)))
            {
                ViewBag.Message = "You are restricted to the highlighted grid.";
                //var userType2 = User.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;

                return View(_userType == "Standard" ? "Standard" : "Pro", game);
            }

            // If the move is valid, make the move, or let chatgpt take its turn if necessary
            if (cellRow == -2 && cellCol == -2 || game.Board[gridRow, gridCol, cellRow, cellCol] == '\0')
            {
                int nextGrid;

                if (game.Mode == Mode.ChatGPT && game.CurrentPlayer == 'O')
                {
                    nextGrid = await MakeChatGPTMove(gridRow, gridCol);
                    if (nextGrid == -1)
                    {
                        ViewBag.Message = $"Player X wins the game!";
                        game.GameWinner = 'X';
                        game.Status = Status.Complete;
                        if (game.Mode == Mode.ChatGPT)
                        {
                            //await IncrementWins();
                        }
                    }
                }
                else
                {
                    game.MakeMove(gridRow, gridCol, cellRow, cellCol);
                    nextGrid = GetGridIndex(cellRow, cellCol);
                    if (game.Mode == Mode.ChatGPT) ViewBag.Message = "ChatGPT is thinking...";
                }
              
                if (!IsGridAvailable(nextGrid))
                {
                    nextGrid = GetRandomAvailableGrid(); // this will be -1 if there are no moves available
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

                char boardWinner = game.CheckBoardWinner();
                if (boardWinner != '\0') // 3 big grids in a row
                {
                    ViewBag.Message = $"Player {boardWinner} wins the game!";
                    game.GameWinner = boardWinner;
                    game.Status = Status.Complete;
                    if (game.Mode == Mode.ChatGPT) {
                        //if (boardWinner == 'X') await IncrementWins();
                        //await IncrementLosses();
                    }
                }
                else if (!IsBoardAvailable()) // no playable cells left
                {
                    int gridsWonByX = 0;
                    int gridsWonByO = 0;
                    for (int x = 0; x < 3; x++) // check second win condition: more grids won
                    {
                        for (int y = 0;  y < 3; y++)
                        {
                            char g = game.CheckGridWinner(x, y);
                            if (g == 'X') gridsWonByX++;
                            if (g == 'O') gridsWonByO++;
                        }
                    }

                    if (gridsWonByX > gridsWonByO)
                    {
                        ViewBag.Message = "Player X wins the game!";
                        game.GameWinner = 'X';
                        //if (game.Mode == Mode.ChatGPT) await IncrementWins();
                    } 
                    else if (gridsWonByO > gridsWonByX)
                    {
                        ViewBag.Message = "Player O wins the game!";
                        game.GameWinner = 'O';
                        //if (game.Mode == Mode.ChatGPT) await IncrementLosses();
                    } 
                    else // only a tie if both players won equal grids
                    {
                        ViewBag.Message = "It's a draw!";
                        game.GameWinner = 'T';
                        if (game.Mode == Mode.ChatGPT)
                        {
                            //await IncrementTies();
                        }
                    }
                    game.Status = Status.Complete;
                }
            }
            else
            {
                ViewBag.Message = "Invalid move. Cell already occupied.";
            }

            //var userType = User.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;

            return View(_userType == "Standard" ? "Standard" : "Pro", game);
        }

        private async Task<int> MakeChatGPTMove(int restrictedGridRow, int restrictedGridCol)
        {
            if (game.CheckBoardWinner() == '\0')
            {
                List<int> gptMove = await _openAIService.GetNextMove(game, (restrictedGridRow, restrictedGridCol));
                game.MakeMove(restrictedGridRow, restrictedGridCol, gptMove[2], gptMove[3]);
                return 3 * gptMove[2] + gptMove[3];
            }
            return -1;
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

                var sentences = response.Split('.');
                response = sentences[0] + "." + sentences[2] + ".";
                ViewBag.Hint = response;
            }

            catch (Exception ex)
            {
                ViewBag.Hint = "Sorry, but we couldn't fetch your hint.";
            }

            //var userType = User.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;

            return View(_userType == "Standard" ? "Standard" : "Pro", game);
        }

        // GET: TTT/Reset
        public ActionResult Reset()
        {
            game.BoardString = new string('\0', 81); // Set all cells to empty
            game.CurrentPlayer = 'X';
            game.GameWinner = '\0';
            game.Status = Status.Active;
            game.Player1Time = game.MaxTime;
            game.Player2Time = game.MaxTime;
            game.RestrictedGrid = null;
            restrictedGridX = null;
            restrictedGridO = null;

            _gameTimerService = new GameTimerService();
            return View(_userType == "Standard" ? "Standard" : "Pro", game);
        }

        // Method to increment wins for the logged-in user
        private async Task IncrementWins()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            user.GamesWon++;
            user.GameHistory.Add(game);
            await _userManager.UpdateAsync(user);
        }

        // Method to increment losses for the logged-in user
        private async Task IncrementLosses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            user.GamesLost++;
            await _userManager.UpdateAsync(user);
        }

        private async Task IncrementTies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            user.GamesTied++;
            await _userManager.UpdateAsync(user);
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

        public async Task<IActionResult> GetGameStatus()
        {
            var gameStatus = new
            {
                Player1Time = game.Player1Time,
                Player2Time = game.Player2Time,
                Message = ViewBag.Message
            };
            return Json(gameStatus);
        }
    }
}
