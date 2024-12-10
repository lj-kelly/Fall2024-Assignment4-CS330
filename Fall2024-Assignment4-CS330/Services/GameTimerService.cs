using Fall2024_Assignment4_CS330.Models;
using System.Security.Cryptography.X509Certificates;
using System.Timers;


namespace Fall2024_Assignment4_CS330.Services
{
    public class GameTimerService
    {
        private readonly Dictionary<int, TTTModel> _activeGames;
        private readonly System.Timers.Timer _timer;
        private int _gameTimerId;

        public GameTimerService()
        {
            _activeGames = new Dictionary<int, TTTModel>();
            _timer = new System.Timers.Timer(50);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _gameTimerId = 0;
        }

        public void AddGame(TTTModel game)
        {
            _activeGames[_gameTimerId++] = game;
        }

        public void RemoveGame(int gameId)
        {
            _activeGames.Remove(gameId);
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var keyValPair in _activeGames)
            {
                var id = keyValPair.Key;
                var game = keyValPair.Value;
                if (game.Status == Status.Complete) continue;

                if (game.CurrentPlayer == 'X') game.Player1Time -= (float)0.05;
                else game.Player2Time -= (float)0.05;

                if (game.Player1Time <= 0 || game.Player2Time <= 0)
                {
                    game.HandleTimeout();
                    this.RemoveGame(id);
                }
                Console.WriteLine($"Player1Time: {game.Player1Time}s. Player2Time: {game.Player2Time})s");
            }
        }
    }
}
