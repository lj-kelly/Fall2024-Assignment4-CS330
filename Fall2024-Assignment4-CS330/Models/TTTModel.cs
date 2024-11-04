using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fall2024_Assignment4_CS330.Models
{
    public class TTTModel
    {
        // Primary key for EF
        [Key]
        public int Id { get; set; }

        // Flattened string representation of the board
        public string BoardString { get; set; } = new string('\0', 9); // Initializes with empty cells

        // Non-mapped property to expose board as a 2D array
        [NotMapped] // EF will ignore this property
        public char[,] Board
        {
            get
            {
                char[,] board = new char[3, 3];
                var flatBoard = BoardString.ToCharArray();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        board[i, j] = flatBoard[i * 3 + j];
                    }
                }
                return board;
            }
            set
            {
                BoardString = string.Join("", value.Cast<char>());
            }
        }

        public char CurrentPlayer { get; set; } = 'X';

        public bool IsCellEmpty(int row, int col)
        {
            return Board[row, col] == '\0';
        }

        public void MakeMove(int row, int col)
        {
            if (IsCellEmpty(row, col))
            {
                var tempBoard = Board;
                tempBoard[row, col] = CurrentPlayer;
                Board = tempBoard; // Update the flattened BoardString
                CurrentPlayer = (CurrentPlayer == 'X') ? 'O' : 'X';
            }
        }

        public char CheckWinner()
        {
            // Check rows, columns, and diagonals for a winner
            for (int i = 0; i < 3; i++)
            {
                if (Board[i, 0] != '\0' && Board[i, 0] == Board[i, 1] && Board[i, 1] == Board[i, 2])
                    return Board[i, 0];
                if (Board[0, i] != '\0' && Board[0, i] == Board[1, i] && Board[1, i] == Board[2, i])
                    return Board[0, i];
            }

            if (Board[0, 0] != '\0' && Board[0, 0] == Board[1, 1] && Board[1, 1] == Board[2, 2])
                return Board[0, 0];
            if (Board[0, 2] != '\0' && Board[0, 2] == Board[1, 1] && Board[1, 1] == Board[2, 0])
                return Board[0, 2];

            return '\0'; // No winner yet
        }

        public bool IsDraw()
        {
            return BoardString.All(cell => cell != '\0') && CheckWinner() == '\0';
        }
    }
}
