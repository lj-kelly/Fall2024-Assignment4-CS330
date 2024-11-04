using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment4_CS330.Data;
using Fall2024_Assignment4_CS330.Models;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class TTTController : Controller
    {
        private static TTTModel game = new TTTModel(); // Simulating a session-level game instance

        // GET: TTT
        public ActionResult Index()
        {
            return View(game);
        }

        // POST: TTT/MakeMove
        [HttpPost]
        public ActionResult MakeMove(int row, int col)
        {
            if (game.IsCellEmpty(row, col))
            {
                game.MakeMove(row, col);
            }

            if (game.CheckWinner() != '\0')
            {
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
    }
}
