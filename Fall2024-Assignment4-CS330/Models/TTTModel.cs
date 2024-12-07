using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Fall2024_Assignment4_CS330.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Fall2024_Assignment4_CS330.Models
{
    public class TTTModel
    {
        [Key]
        public int Id { get; set; }
        public char GameWinner { get; set; } = '\0';

        // Flattened representation of all boards for storage
        public string BoardString { get; set; } = new string('\0', 81); // 9x9 flattened grid
        public string Mode { get; set; } = "Local";
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }
        public int Player1Time { get; set; }
        public int Player2Time { get; set; }
        public char CurrentPlayer { get; set; } = 'X';
        public int? RestrictedGrid { get; set; }

        [NotMapped]
        public char[,,,] Board
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

        public void MakeMove(int outerRow, int outerCol, int innerRow, int innerCol)
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

        public char UpdateTime()
        {
            if (CurrentPlayer == 'X')
            {
                Player1Time--;
                if (Player1Time <= 0)
                    return 'X';
            }

            else
            {
                Player2Time--;
                if (Player2Time <= 0)
                    return 'O';
            }

            return '\0';
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