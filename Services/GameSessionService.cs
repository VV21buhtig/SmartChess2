using SmartChess.Models.Chess;
using SmartChess.Models.Chess.Enums;
using SmartChess.Models.Entities;
using System.Threading.Tasks;

namespace SmartChess.Services
{
    public class GameSessionService
    {
        public Board CurrentBoard { get; private set; } = new Board();
        public Models.Chess.Enums.Color CurrentPlayer { get; private set; } = Models.Chess.Enums.Color.White;
        public Models.Chess.Enums.GameState GameState { get; private set; } = Models.Chess.Enums.GameState.InProgress;

        private readonly IChessEngine _chessEngine;
        private readonly DatabaseService _databaseService;
        private User? _currentUser;
        private Game? _currentGame;

        public GameSessionService(IChessEngine chessEngine, DatabaseService databaseService)
        {
            _chessEngine = chessEngine;
            _databaseService = databaseService;
        }

        public async Task InitializeGame()
        {
            _chessEngine.InitializeGame(); // Вызов метода из IChessEngine
            CurrentBoard = _chessEngine.CurrentBoard;
            CurrentPlayer = _chessEngine.CurrentPlayer;
            GameState = _chessEngine.GameState;

            if (_currentUser != null)
            {
                _currentGame = new Game { UserId = _currentUser.Id };
                _currentGame = await _databaseService.CreateGameAsync(_currentGame);
            }
        }

        public async Task StartNewGameAsync(User user)
        {
            _currentUser = user;
            InitializeGame(); // Используем внутренний метод
            // ... (логика создания игры в БД)
        }

        public async Task<bool> MakeMoveAsync(Position from, Position to)
        {
            if (_currentGame == null)
            {
                System.Diagnostics.Trace.WriteLine("=== ERROR: No current game ===");
                return false; // Игра не начата
            }

            // Вызов метода из ChessEngine
            bool moveSuccess = await _chessEngine.MakeMoveAsync(from, to);
            System.Diagnostics.Trace.WriteLine($"ChessEngine move success: {moveSuccess}");

            if (moveSuccess)
            {
                // Обновление состояния сессии
                CurrentBoard = _chessEngine.CurrentBoard;
                CurrentPlayer = _chessEngine.CurrentPlayer;
                GameState = _chessEngine.GameState;
                System.Diagnostics.Trace.WriteLine("=== MOVE SUCCESS IN SESSION ===");
                
                // Запись хода в БД
                var move = new Move 
                { 
                    GameId = _currentGame.Id, 
                    FromPosition = from.ToString(), 
                    ToPosition = to.ToString(),
                    MoveNumber = _currentGame.MoveCount + 1,
                    PieceType = GetPieceTypeAtPosition(from), // Получаем тип фигуры
                    Color = CurrentPlayer == Models.Chess.Enums.Color.White ? "White" : "Black",
                    IsCapture = IsCaptureMove(from, to),
                    CapturedPiece = GetCapturedPieceType(from, to)
                };
                
                await _databaseService.CreateMoveAsync(move);
                _currentGame.MoveCount++; // Увеличиваем счетчик ходов
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("=== MOVE FAILED IN SESSION ===");
            }

            return moveSuccess;
        }

        public async Task<Models.Chess.Enums.GameState> GetGameStateAsync()
        {
            return await _chessEngine.GetGameStateAsync();
        }

        private string GetPieceTypeAtPosition(Position position)
        {
            var piece = CurrentBoard[position.Row, position.Col];
            if (piece == null || piece.Type == Models.Chess.Enums.PieceType.Empty)
                return "Empty";

            return piece.Type.ToString();
        }

        private bool IsCaptureMove(Position from, Position to)
        {
            var targetPiece = CurrentBoard[to.Row, to.Col];
            return targetPiece != null && targetPiece.Type != Models.Chess.Enums.PieceType.Empty;
        }

        private string? GetCapturedPieceType(Position from, Position to)
        {
            var targetPiece = CurrentBoard[to.Row, to.Col];
            if (targetPiece != null && targetPiece.Type != Models.Chess.Enums.PieceType.Empty)
                return targetPiece.Type.ToString();
            
            return null;
        }
    }
}