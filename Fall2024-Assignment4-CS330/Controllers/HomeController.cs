using Fall2024_Assignment4_CS330.Data;
using Fall2024_Assignment4_CS330.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // every time the page loads, remove games that failed, been in the queue 5+ mins, or been active too long
            _context.Database.ExecuteSqlRaw("EXEC CleanDeadGames");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult QueueHandler()
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to play online.";
                return RedirectToAction("Index");
            }

            var existingGame = FindGameInQueue();

            if (existingGame.Status == Status.Queued)
            {
                existingGame.Player2Id = userId;
                existingGame.Status = Status.Active;

                UpdateGameInDb(existingGame);
                NotifyPlayer(existingGame.Id);
                return View("../TTT/Index", existingGame);
            }

            TTTModel game = AddGameToDb(userId, null, null, Publicity.Public, Status.Queued, 15);
            return View("../TTT/Index", game);
        }

        public TTTModel AddGameToDb(string player1Id, string? player2Id, string? gameCode, Publicity publicity, Status status, int maxTime)
        { // only to be used with online games
            TTTModel game = new TTTModel
            {
                Mode = "Online",
                Player1Id = player1Id,
                Player2Id = player2Id, // if the game has 1 player (online, 1 sided currently), player2id is -1
                JoinCode = gameCode,
                Publicity = publicity,
                Status = status,
                MaxTime = maxTime,
                Player1Time = maxTime*60,
                Player2Time = maxTime*60,
                GameCreationTime = DateTime.Now
        };

            _context.TTTModel.Add(game);
            _context.SaveChanges();
            return game;
        }

        public void UpdateGameInDb(TTTModel game)
        {
            _context.TTTModel.Update(game);
            _context.SaveChanges();
            return;
        }

        public TTTModel FindGameInQueue()
        {
            var games = _context.TTTModel;
            return games.FirstOrDefault(
                g => g.Publicity == Publicity.Public && g.Status == Status.Queued)
                ?? new TTTModel { Status = Status.Failed };
        }

        public IActionResult CreateLobbyHandler(string gameCode, int maxTime)
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to play online.";
                return RedirectToAction("Index");
            }

            var existingGame = FindPrivateGame(gameCode);
            if (existingGame.Status == Status.Active)
            {
                TempData["ErrorMessage"] = "This game is full. Please use a different code...";
                return RedirectToAction("Index");
            }
            if (existingGame.Status == Status.Queued)
            {
                TempData["ErrorMessage"] = "This code is already in use. Would you like to join the game?";
                return RedirectToAction("Index");
            }

            TTTModel game = AddGameToDb(userId, null, gameCode, Publicity.Private, Status.Queued, maxTime);
            return View("../TTT/Index", game);
        }

        public IActionResult JoinLobbyHandler(string joinCode)
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to play online.";
                return RedirectToAction("Index");
            }

            var existingGame = FindPrivateGame(joinCode);
            if (existingGame.Status == Status.Active)
            {
                TempData["ErrorMessage"] = "This game is full. Please join a different game...";
                return RedirectToAction("Index");
            }
            if (existingGame.Status == Status.Failed || existingGame.Status == Status.Complete) // game does not exist
            {
                TempData["ErrorMessage"] = "This game code does not exist. Would you like to create a lobby?";
                return RedirectToAction("Index");
            }

            existingGame.Player2Id = userId;
            existingGame.Status = Status.Active;

            UpdateGameInDb(existingGame);
            NotifyPlayer(existingGame.Id);
            return View("../TTT/Index", existingGame);
        }

        public TTTModel FindPrivateGame(string gameCode) // find any private games with this password
        {
            // active or queued returned normally
            // finished or empty counts as failed search
            var games = _context.TTTModel;
            return games.FirstOrDefault(
                g => g.Publicity == Publicity.Private && g.JoinCode == gameCode && g.Status != Status.Complete) 
                ?? new TTTModel { Status = Status.Failed };
        }

        public void NotifyPlayer(int gameId)
        {
            // find game with this id
            // send websocket notification to other player
            // other player's site should refresh page when request received
            return;
        }

        private void DatabaseDebug()
        {
            /* create table
            var buildMyTable = @"
            CREATE TABLE TTTGames (
            Id INT PRIMARY KEY IDENTITY(1,1),
            Mode NVARCHAR(50) NOT NULL,
            Player1Id NVARCHAR(50) NULL,
            Player2Id NVARCHAR(50) NULL,
            JoinCode NVARCHAR(50) NULL,
            Publicity INT NOT NULL,
            Status INT NOT NULL,
            MaxTime INT NOT NULL,
            GameStartTime DATETIME NULL,
            BoardString NVARCHAR(81) NOT NULL,
            CurrentPlayer CHAR(1) NOT NULL,
            RestrictedGrid INT NULL,
            GameWinner CHAR(1) NOT NULL
            );";
            _context.Database.ExecuteSqlRaw(buildMyTable);
            */

            // fix the table?
            string sql = @"
            ALTER TABLE TTTGames ADD
                Mode NVARCHAR(MAX) NOT NULL DEFAULT 'Online',
                Player1Id NVARCHAR(MAX) NULL,
                Player2Id NVARCHAR(MAX) NULL,
                JoinCode NVARCHAR(MAX) NULL,
                Publicity INT NOT NULL DEFAULT 0,
                Status INT NOT NULL DEFAULT 0,
                MaxTime INT NOT NULL DEFAULT 0,
                GameStartTime DATETIME NULL,
                BoardString NVARCHAR(81) NOT NULL DEFAULT REPLICATE('\0', 81),
                CurrentPlayer CHAR(1) NOT NULL DEFAULT 'X',
                RestrictedGrid INT NULL,
                GameWinner CHAR(1) NOT NULL DEFAULT '\0';";

            try
            {
                // Execute the SQL command
                _context.Database.ExecuteSqlRaw(sql);
                //return Content("Table schema updated successfully.");
            }
            catch (Exception ex)
            {
                var a = 1;
                //return Content($"An error occurred: {ex.Message}");
            }
        }
    }
}