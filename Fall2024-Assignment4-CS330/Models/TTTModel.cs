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

        // Game mode: "Local", "Online", "ChatGPT"
        public string Mode { get; set; } = "Local";

        // Players for Online Mode
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }

        // Track current player
        public char CurrentPlayer { get; set; } = 'X';

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

        // Check if a specific cell is empty
        public bool IsCellEmpty(int row, int col)
        {
            return Board[row, col] == '\0';
        }

        // Make a move on the board
        public void MakeMove(int row, int col)
        {
            if (IsCellEmpty(row, col))
            {
                var tempBoard = Board;
                tempBoard[row, col] = CurrentPlayer;
                Board = tempBoard; // Update the flattened BoardString
                TogglePlayer();
            }
        }

        // Toggle the current player
        public void TogglePlayer()
        {
            CurrentPlayer = (CurrentPlayer == 'X') ? 'O' : 'X';
        }

        // Check if there's a winner
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

        // Check if the game is a draw
        public bool IsDraw()
        {
            return BoardString.All(cell => cell != '\0') && CheckWinner() == '\0';
        }

        // Get the best move for ChatGPT (AI)
        public (int row, int col) GetBestMove()
        {
            // Simple AI: First available move (can be replaced with a smarter algorithm like Minimax)
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (IsCellEmpty(i, j))
                    {
                        return (i, j);
                    }
                }
            }
            return (-1, -1); // No move available
        }

        // Initialize players for online mode
        public void InitializePlayers(string player1Id, string player2Id)
        {
            Player1Id = player1Id;
            Player2Id = player2Id;
            CurrentPlayer = 'X';
        }

        // Check if it's the current user's turn in online mode
        public bool IsCurrentUserTurn(string userId)
        {
            if (Mode != "Online")
                return false;
            return (CurrentPlayer == 'X' && userId == Player1Id) || (CurrentPlayer == 'O' && userId == Player2Id);
        }
    }
}
