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

        private async Task<string> getChatResponse(TTTModel board, bool redo, string lastMove)
        {
            List<ChatMessage> messages;

            if (!redo)
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are an expert Tic Tac Toe Player. You will receive the shape you are playing (X or O) and the current board state. Your task is to calculate the optimal move and return ONLY the coordinates in the format `row,column` without any explanation or additional text."),
                    new UserChatMessage($"You are playing: '{board.CurrentPlayer}'. This is the board state:'{board.BoardString}'. Please provide the coordinates of your next move.")
                };
            }
            else
            {
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are an expert Tic Tac Toe Player. You will receive the shape you are playing (X or O) and the current board state. Your task is to calculate the optimal move and return ONLY the coordinates in the format `row,column` without any explanation or additional text."),
                    new UserChatMessage($"Do not make the move '{lastMove}'. You are playing: '{board.CurrentPlayer}'. This is the board state:'{board.BoardString}'. Please provide the coordinates of your next move.")
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

        private bool IsValidMove(List<int> moveCords, TTTModel board)
        {
            if (moveCords == null || moveCords.Count != 2)
                return false;

            int row = moveCords[0];
            int col = moveCords[1];

            return row >= 0 && row <= 2 && col >= 0 && col <= 2 && board.IsCellEmpty(row, col);
        }

        private List<int> ParseCoordinates(string response)
        {
            var match = Regex.Match(response, @"^\s*(\d),\s*(\d)\s*$");
            if (match.Success)
            {
                return new List<int>
                {
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value)
                };
            }

            return null; // Invalid format
        }

        private List<int> GetRandomMove(TTTModel board)
        {
            // Collect all available cells (empty cells) on the board
            List<List<int>> availableMoves = new List<List<int>>();

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (board.IsCellEmpty(row, col))
                    {
                        availableMoves.Add(new List<int> { row, col });
                    }
                }
            }

            // If no available moves, return null
            if (availableMoves.Count == 0)
            {
                return null;
            }

            // Pick a random move from the available moves
            int randomIndex = _random.Next(availableMoves.Count);
            return availableMoves[randomIndex];
        }

        public async Task<List<int>> GetNextMove(TTTModel board)
        {
            List<int> cords = null;
            string lastMove = "";
            string fullResponse = await getChatResponse(board, false, lastMove);

            if (!IsValidMove(cords, board))
            {
                //Makes retry calls a max of 3 times
                for (int i = 0; i < 3; i++)
                {
                    if (!string.IsNullOrEmpty(fullResponse))
                    {
                        cords = ParseCoordinates(fullResponse);
                    }

                    if (cords == null || !IsValidMove(cords, board))
                    {
                        lastMove = fullResponse; // Record invalid move for redo
                        fullResponse = await getChatResponse(board, true, lastMove);
                    }

                    if(IsValidMove(cords, board))
                    {
                        return cords;
                    }
                }
            }

            // If GPT cannot return a valid response, then make a random move
            cords = GetRandomMove(board);

            return cords;
        }
    }
}
