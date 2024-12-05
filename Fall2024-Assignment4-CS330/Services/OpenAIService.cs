using Microsoft.AspNetCore.Mvc;
using Azure.AI.OpenAI;
using Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Fall2024_Assignment4_CS330.Models;
using OpenAI.Chat;
using System.ClientModel;
using Microsoft.AspNetCore.Components.Web;
using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Composition;
using System.Reflection;
using System.Security.Policy;

namespace Fall2024_Assignment4_CS330.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly string openAIKey;
        private readonly string openAIEndpoint;
        private readonly Random _random;

        public OpenAIService(IConfiguration config)
        {
            openAIKey = config["OpenAIKey"];
            openAIEndpoint = config["OpenAIEndpoint"];
            _random = new Random();

            if (string.IsNullOrEmpty(openAIKey) || string.IsNullOrEmpty(openAIEndpoint))
            {
                throw new ArgumentNullException("OpenAI key or endpoint not provided in configuration.");
            }

            AzureOpenAIClient azureClient = new(
                new Uri(openAIEndpoint),
                new ApiKeyCredential(openAIKey));
            var chatClient = azureClient.GetChatClient("gpt-35-turbo");

            _chatClient = chatClient;
        }

        private async Task<string> getChatResponse(TTTModel board, bool redo, string lastMove, (int outerRow, int outerCol) restrictedGrid)
        {
            List<ChatMessage> messages;

            string skillLevel;

            /*
            switch (difficulty)
            {
                case "Easy":
                    skillLevel = "amateur";
                    break;

                case "Medium":
                    skillLevel = "average";
                    break;
                case "Hard":
                    skillLevel = "master";
                    break;

                default:
                    skillLevel = "decent";
                    break;
            }

            */
            if (!redo)
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"""You are a master player of Ultimate Tic Tac Toe. You will receive the shape you are playing(X or O), the current board state, and the restricted grid you are allowed to play in. Ultimate Tic Tac Toe rules apply:
                    -Players alternate turns.
                    -Moves must be made in the restricted grid unless it is unavailable(fully filled or has a winner).
                    -You will return the best possible move within the restricted grid, taking into consideration both the current grid and the entire board. ONLY return the coordinates in the format `row,column` without any explanation or additional text."""),
                    new UserChatMessage($"You are playing: '{board.CurrentPlayer}'. This is the board state:'{board.BoardString}'. Your restricted grid is the grid at row {restrictedGrid.outerRow}, column {restrictedGrid.outerCol}. Please provide the coordinates of the optimal move.")
                };
            }
            else
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"""You are a master player of Ultimate Tic Tac Toe. You will receive the shape you are playing(X or O), the current board state, and the restricted grid you are allowed to play in. Ultimate Tic Tac Toe rules apply:
                    -Players alternate turns.
                    -Moves must be made in the restricted grid unless it is unavailable(fully filled or has a winner).
                    -You will return the best possible move within the restricted grid, taking into consideration both the current grid and the entire board. ONLY return the coordinates in the format `row,column` without any explanation or additional text."""),
                    new UserChatMessage($"Do not make the move '{lastMove}'. You are playing: '{board.CurrentPlayer}'. This is the board state:'{board.BoardString}'. Your restricted grid is the grid at row {restrictedGrid.outerRow}, column {restrictedGrid.outerCol}. Please provide the coordinates of the optimal move.")
                };
            }

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f
            };

            try
            {
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

                if (completion.Content != null)
                {
                    string fullResponse = completion.Content.First().Text.Trim();
                    return fullResponse;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching chat response: " + ex.Message);
            }

            return null;
        }

        private bool IsValidMove(List<int> moveCords, TTTModel board, (int outerRow, int outerCol) restrictedGrid)
        {
            if (moveCords == null || moveCords.Count != 4)
                return false;

            int outerRow = moveCords[0];
            int outerCol = moveCords[1];
            int innerRow = moveCords[2];
            int innerCol = moveCords[3];

            // Check if move is within the restricted grid and if the cell is empty
            return outerRow == restrictedGrid.outerRow &&
                   outerCol == restrictedGrid.outerCol &&
                   innerRow >= 0 && innerRow <= 2 &&
                   innerCol >= 0 && innerCol <= 2 &&
                   board.IsCellEmpty(outerRow, outerCol, innerRow, innerCol);
        }


        private List<int> ParseCoordinates(string response)
        {
            var match = Regex.Match(response, @"^\s*(\d),\s*(\d),\s*(\d),\s*(\d)\s*$");
            if (match.Success)
            {
                return new List<int>
        {
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            int.Parse(match.Groups[4].Value)
        };
            }

            return null; // Invalid format
        }


        private List<int> GetRandomMove(TTTModel board, (int outerRow, int outerCol) restrictedGrid)
        {
            List<List<int>> availableMoves = new List<List<int>>();

            // Check if the restricted grid is valid
            if (board.IsGridPlayable(restrictedGrid.outerRow, restrictedGrid.outerCol))
            {
                // Collect available cells in the restricted grid
                for (int innerRow = 0; innerRow < 3; innerRow++)
                {
                    for (int innerCol = 0; innerCol < 3; innerCol++)
                    {
                        if (board.IsCellEmpty(restrictedGrid.outerRow, restrictedGrid.outerCol, innerRow, innerCol))
                        {
                            availableMoves.Add(new List<int> { restrictedGrid.outerRow, restrictedGrid.outerCol, innerRow, innerCol });
                        }
                    }
                }
            }
            else
            {
                // Collect all available cells across all grids
                for (int outerRow = 0; outerRow < 3; outerRow++)
                {
                    for (int outerCol = 0; outerCol < 3; outerCol++)
                    {
                        if (board.IsGridPlayable(outerRow, outerCol))
                        {
                            for (int innerRow = 0; innerRow < 3; innerRow++)
                            {
                                for (int innerCol = 0; innerCol < 3; innerCol++)
                                {
                                    if (board.IsCellEmpty(outerRow, outerCol, innerRow, innerCol))
                                    {
                                        availableMoves.Add(new List<int> { outerRow, outerCol, innerRow, innerCol });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If no available moves, return null
            if (availableMoves.Count == 0)
                return null;

            // Pick a random move
            int randomIndex = _random.Next(availableMoves.Count);
            return availableMoves[randomIndex];
        }



        public async Task<List<int>> GetNextMove(TTTModel board, (int outerRow, int outerCol) restrictedGrid)
        {
            List<int> cords = null;
            string lastMove = "";
            string fullResponse = await getChatResponse(board, false, lastMove, restrictedGrid);

            if (!IsValidMove(cords, board, restrictedGrid))
            {
                // Retry up to 3 times
                for (int i = 0; i < 3; i++)
                {
                    if (!string.IsNullOrEmpty(fullResponse))
                    {
                        cords = ParseCoordinates(fullResponse);
                    }

                    if (cords == null || !IsValidMove(cords, board, restrictedGrid))
                    {
                        lastMove = fullResponse; // Record invalid move for retry
                        fullResponse = await getChatResponse(board, true, lastMove, restrictedGrid);
                    }

                    if (IsValidMove(cords, board, restrictedGrid))
                    {
                        return cords;
                    }
                }
            }

            // If GPT fails to provide a valid move, pick a random one
            cords = GetRandomMove(board, restrictedGrid);

            return cords;
        }
        public async Task<string> GetHint(TTTModel board)
        {
            Console.WriteLine("Getting hint");
            (int outerRow, int outerCol) restrictedGrid = board.RestrictedGrid.HasValue
            ? (board.RestrictedGrid.Value / 3, board.RestrictedGrid.Value % 3)
            : (-1, -1);

            // Request a move and explanation from GPT

            List<int> gptMoves = await GetNextMove(board, restrictedGrid);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"""You are a master player of Ultimate Tic Tac Toe. You will receive the shape you are playing(X or O), the current board state, the restricted grid you are allowed to play in, and the row and column of the move you will play.. Ultimate Tic Tac Toe rules apply:
                    -Players alternate turns.
                    -Moves must be made in the restricted grid unless it is unavailable(fully filled or has a winner).
                    -You will give a short description on why this is the optimal move, taking into consideration both the current grid and the entire board. ONLY return the coordinates in the format `row,column` without any explanation or additional text."""),

                    new UserChatMessage($@"""You are playing: '{board.CurrentPlayer}'. This is the board state:'{board.BoardString}'. 
                    Your restricted grid is the grid at row {restrictedGrid.outerRow}, column {restrictedGrid.outerCol}. 
                    Your move within the restricted grid is at row {gptMoves[0]}, column {gptMoves[1]}. Please give a short description on why this is the optimal move.""")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f
            };

            string explanation = string.Empty;
            try
            {
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

                if (completion.Content != null)
                {
                    explanation = completion.Content.First().Text.Trim();
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error fetching chat response: " + ex.Message);
            }


            // Step 4: Format the response
            return $"Hint: Move to row {gptMoves[0] + 1}, column {gptMoves[1] + 1}. {explanation}";
        }
    }
}
