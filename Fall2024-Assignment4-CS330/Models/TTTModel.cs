using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Fall2024_Assignment4_CS330.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
namespace Fall2024_Assignment4_CS330.Models
{
    public enum Mode
    {
        Local,
        ChatGPT
    }

    public enum Status
    {
        Queued,
        Active,
        Complete,
        Failed
    }

    public class TTTModel
    {
        /// game metadata
        /// fields that describe the game
        [Key]
        public int Id { get; set; }
        public Mode Mode { get; set; } = Mode.Local;
        public string? PlayerId { get; set; }
        public Status Status { get; set; } = Status.Active;
        public int MaxTime { get; set; } = 10; // in minutes
        public DateTime? GameCreationTime { get; set; } = null;
        public char GameWinner { get; set; } = '\0'; // empty, x, o, or t for a tied game (rare)

        /// Game data
        /// Fields that effect the game itself and update frequently
        public string BoardString { get; set; } = new string('\0', 81); // flattened representation of the board 
        public char CurrentPlayer { get; set; } = 'X'; // always x or o, x is first and player 1
        public int? RestrictedGrid { get; set; } = null; // the index of the grid that the current player has to play in
        public float Player1Time { get; set; } = 600; // in seconds
        public float Player2Time { get; set; } = 600; // in seconds

        [NotMapped]
        public char[,,,] Board // 4d matrix of cells, referred to by the char of the claimer
        {
            get
            {
                char[,,,] board = new char[3, 3, 3, 3];
                var flatBoard = BoardString.ToCharArray();
                int index = 0;

                for (int outerRow = 0; outerRow < 3; outerRow++)
                {
                    for (int outerCol = 0; outerCol < 3; outerCol++)
                    {
                        for (int innerRow = 0; innerRow < 3; innerRow++)
                        {
                            for (int innerCol = 0; innerCol < 3; innerCol++)
                            {
                                board[outerRow, outerCol, innerRow, innerCol] = flatBoard[index++];
                            }
                        }
                    }
                }
                return board;
            }
            set
            {
                BoardString = string.Join("", value.Cast<char>());
            }
        }

        public void MakeMove(int outerRow, int outerCol, int innerRow, int innerCol) // update the board 
        {
            if (Board[outerRow, outerCol, innerRow, innerCol] == '\0')
            {
                var tempBoards = Board;
                tempBoards[outerRow, outerCol, innerRow, innerCol] = CurrentPlayer;
                Board = tempBoards;
                TogglePlayer();
            }
        }

        public void TogglePlayer()
        {
            CurrentPlayer = (CurrentPlayer == 'X') ? 'O' : 'X';
        }

        public char CheckGridWinner(int outerRow, int outerCol)
        {
            char[,] grid = new char[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    grid[i, j] = Board[outerRow, outerCol, i, j];

            // Check rows, columns, and diagonals
            for (int i = 0; i < 3; i++)
            {
                if (grid[i, 0] != '\0' && grid[i, 0] == grid[i, 1] && grid[i, 1] == grid[i, 2])
                    return grid[i, 0];
                if (grid[0, i] != '\0' && grid[0, i] == grid[1, i] && grid[1, i] == grid[2, i])
                    return grid[0, i];
            }

            if (grid[0, 0] != '\0' && grid[0, 0] == grid[1, 1] && grid[1, 1] == grid[2, 2])
                return grid[0, 0];
            if (grid[0, 2] != '\0' && grid[0, 2] == grid[1, 1] && grid[1, 1] == grid[2, 0])
                return grid[0, 2];

            return '\0'; // No winner
        }

        public char CheckBoardWinner()
        {
            char[,] board = new char[3, 3];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    board[i, j] = CheckGridWinner(i, j);

            // Check the big board's rows, columns, and diagonals
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] != '\0' && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
                    return board[i, 0];
                if (board[0, i] != '\0' && board[0, i] == board[1, i] && board[1, i] == board[2, i])
                    return board[0, i];
            }

            if (board[0, 0] != '\0' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
                return board[0, 0];
            if (board[0, 2] != '\0' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
                return board[0, 2];

            return '\0'; // No winner yet
        }

        public bool IsCellEmpty(int outerRow, int outerCol, int innerRow, int innerCol)
        {
            return Board[outerRow, outerCol, innerRow, innerCol] == '\0';
        }

        public bool IsGridPlayable(int outerRow, int outerCol)
        {
            // Check if the grid has a winner
            if (CheckGridWinner(outerRow, outerCol) != '\0')
            {
                return false;
            }

            // Check if there are any empty cells in the grid
            for (int innerRow = 0; innerRow < 3; innerRow++)
            {
                for (int innerCol = 0; innerCol < 3; innerCol++)
                {
                    if (IsCellEmpty(outerRow, outerCol, innerRow, innerCol))
                    {
                        return true; // Grid is playable if there's at least one empty cell
                    }
                }
            }

            return false; // Grid is not playable if no empty cells are found
        }
    }
}