# SmartChess

SmartChess - это приложение для игры в шахматы с возможностью регистрации пользователей, сохранения истории игр и анализа ходов. Приложение разработано на .NET 8 с использованием WPF и шаблона MVVM.

## Структура проекта

```
/workspace/
├── Algorithms/
│   ├── GameAnalyzer.cs
│   ├── MoveCalculator.cs
│   └── PathFinder.cs
├── Commands/
│   └── RelayCommand.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   └── Repository/
├── Models/
│   ├── Chess/
│   └── Entities/
├── Resources/
│   ├── Converters/
│   ├── Images/
│   └── Styles/
├── Services/
│   ├── AlgorithmService.cs
│   ├── AuthService.cs
│   ├── ChessEngine.cs
│   ├── DatabaseService.cs
│   └── GameSessionService.cs
├── Utilities/
│   ├── ChessNotationConverter.cs
│   ├── Config.cs
│   ├── Extensions.cs
│   ├── PasswordBoxAssistant.cs
│   └── Validator.cs
├── ViewModels/
│   ├── AuthViewModel.cs
│   ├── ChessSquareViewModel.cs
│   ├── GameViewModel.cs
│   ├── HistoryViewModel.cs
│   ├── MainViewModel.cs
│   └── ProfileViewModel.cs
├── Views/
│   ├── Components/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   └── UserControls/
├── App.xaml
├── App.xaml.cs
├── AssemblyInfo.cs
├── SmartChess.csproj
└── SmartChess.sln
```

## Описание файлов

### Algorithms/GameAnalyzer.cs
```csharp
using SmartChess.Models.Chess;
using SmartChess.Models.Chess.Enums;
using System.Threading.Tasks;

namespace SmartChess.Algorithms
{
    public class GameAnalyzer
    {
        public async Task<int> AnalyzeDepthAsync(Board board, int depth)
        {
            if (depth <= 0)
            {
                return 1; 
            }

            int totalPositions = 0;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var piece = board[x, y];
                    if (piece != null)
                    {
                        var moves = piece.GetPossibleMoves(board);
                        foreach (var move in moves)
                        {
                            var capturedPiece = board[move];
                            board[move] = piece;
                            board[piece.Position] = null;
                            piece.MoveTo(move);

                            totalPositions += await AnalyzeDepthAsync(board, depth - 1);

                            board[piece.Position] = piece;
                            piece.MoveTo(new Position(x, y));
                            board[move] = capturedPiece;
                        }
                    }
                }
            }
            return totalPositions;
        }
    }
}
```

### Services/ChessEngine.cs
```csharp
using SmartChess.Models.Chess;
using SmartChess.Models.Chess.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartChess.Services
{
    public class ChessEngine : IChessEngine
    {
        public Board CurrentBoard { get; private set; } = new Board();
        public Color CurrentPlayer { get; private set; } = Color.White;
        public GameState GameState { get; private set; } = GameState.InProgress;

        public void InitializeGame()
        {
            CurrentBoard.InitializeStandardPosition();
            CurrentPlayer = Color.White;
            GameState = GameState.InProgress;
        }

        public async Task<bool> MakeMoveAsync(Position from, Position to)
        {
            System.Diagnostics.Trace.WriteLine($"=== CHESS ENGINE MOVE: {from} -> {to} ===");

            var piece = CurrentBoard[from];
            if (piece == null)
            {
                System.Diagnostics.Trace.WriteLine("=== ERROR: No piece at from position ===");
                return false;
            }

            if (piece.Color != CurrentPlayer)
            {
                System.Diagnostics.Trace.WriteLine($"=== ERROR: Wrong color. Piece: {piece.Color}, Current: {CurrentPlayer} ===");
                return false;
            }

            if (!piece.IsValidMove(to, CurrentBoard))
            {
                System.Diagnostics.Trace.WriteLine("=== ERROR: Invalid move ===");
                return false;
            }

            System.Diagnostics.Trace.WriteLine("=== MOVE VALID - EXECUTING ===");

            var capturedPiece = CurrentBoard[to];
            CurrentBoard[to] = piece;
            CurrentBoard[from] = null;
            piece.MoveTo(to);

            bool inCheck = CurrentBoard.IsInCheck(CurrentPlayer);
            if (inCheck)
            {
                System.Diagnostics.Trace.WriteLine("=== ERROR: Move puts king in check ===");
                CurrentBoard[from] = piece;
                CurrentBoard[to] = capturedPiece;
                piece.MoveTo(from);
                return false;
            }

            CurrentPlayer = CurrentPlayer == Color.White ? Color.Black : Color.White;
            System.Diagnostics.Trace.WriteLine($"=== MOVE SUCCESS! New player: {CurrentPlayer} ===");

            // Обновление состояния игры
            if (CurrentBoard.IsCheckmate(CurrentPlayer))
            {
                GameState = GameState.Checkmate;
            }
            else if (CurrentBoard.IsStalemate(CurrentPlayer))
            {
                GameState = GameState.Stalemate;
            }
            else
            {
                GameState = GameState.InProgress;
            }

            return true;
        }
        //public async Task<bool> MakeMoveAsync(Position from, Position to)
        //{
        //    System.Diagnostics.Trace.WriteLine($"=== CHESS ENGINE MOVE: {from} -> {to} ===");
        //    var piece = CurrentBoard[from];
        //    if (piece == null || piece.Color != CurrentPlayer || !piece.IsValidMove(to, CurrentBoard))
        //    {
        //        System.Diagnostics.Trace.WriteLine("=== ERROR: No piece at from position ===");
        //        return false;
        //    }

        //    var capturedPiece = CurrentBoard[to];
        //    CurrentBoard[to] = piece;
        //    CurrentBoard[from] = null;
        //    piece.MoveTo(to);

        //    bool inCheck = CurrentBoard.IsInCheck(CurrentPlayer);
        //    if (inCheck)
        //    {
        //        CurrentBoard[from] = piece;
        //        CurrentBoard[to] = capturedPiece;
        //        piece.MoveTo(from);
        //        return false;
        //    }

        //    CurrentPlayer = CurrentPlayer == Color.White ? Color.Black : Color.White;

        //    if (CurrentBoard.IsCheckmate(CurrentPlayer))
        //    {
        //        GameState = GameState.Checkmate;
        //    }
        //    else if (CurrentBoard.IsStalemate(CurrentPlayer))
        //    {
        //        GameState = GameState.Stalemate;
        //    }
        //    else
        //    {
        //        GameState = GameState.InProgress;
        //    }

        //    return true;
        //}

        public async Task<List<Position>> GetPossibleMovesAsync(Position position)
        {
            var piece = CurrentBoard[position];
            if (piece == null || piece.Color != CurrentPlayer)
            {
                return new List<Position>();
            }
            var possibleMoves = piece.GetPossibleMoves(CurrentBoard);
            var filteredMoves = new List<Position>();

            foreach (var move in possibleMoves)
            {
                var capturedPiece = CurrentBoard[move];
                CurrentBoard[move] = piece;
                CurrentBoard[piece.Position] = null;
                piece.MoveTo(move);

                if (!CurrentBoard.IsInCheck(CurrentPlayer))
                {
                    filteredMoves.Add(move);
                }

                CurrentBoard[piece.Position] = piece;
                piece.MoveTo(position);
                CurrentBoard[move] = capturedPiece;
            }

            return filteredMoves;
        }

        public async Task<GameState> GetGameStateAsync()
        {
            return GameState;
        }

        public async Task<Board> GetCurrentBoardStateAsync()
        {
            return CurrentBoard;
        }
    }
}
```

### ViewModels/MainViewModel.cs
```csharp
// ViewModels/MainViewModel.cs
using SmartChess.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartChess.Models.Entities;
using SmartChess.Views.UserControls;

namespace SmartChess.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        private User? _currentUser;
        private GameSessionService _gameSessionService;

        // Изменён конструктор - принимает все VM
        public MainViewModel(AuthViewModel authViewModel, GameViewModel gameViewModel, HistoryViewModel historyViewModel, ProfileViewModel profileViewModel, GameSessionService gameSessionService)
        {
            AuthViewModel = authViewModel;
            GameViewModel = gameViewModel;
            HistoryViewModel = historyViewModel;
            ProfileViewModel = profileViewModel;
            _gameSessionService = gameSessionService;

            NavigateToGameCommand = new RelayCommand(() => CurrentView = GameViewModel);
            NavigateToHistoryCommand = new RelayCommand(() => CurrentView = HistoryViewModel);
            NavigateToProfileCommand = new RelayCommand(() => CurrentView = ProfileViewModel);
            NavigateToAuthCommand = new RelayCommand(() => CurrentView = AuthViewModel);


            // Подписываемся на события из AuthViewModel
            AuthViewModel.LoginSuccess += OnAuthLoginSuccess;
            AuthViewModel.RegistrationSuccess += OnAuthRegistrationSuccess;
            
            // Подписываемся на события навигации из GameViewModel
            GameViewModel.NavigateToHistoryRequested += () => CurrentView = HistoryViewModel;
            GameViewModel.NavigateToProfileRequested += () => CurrentView = ProfileViewModel;

            // Начинаем с экрана авторизации
            CurrentView = AuthViewModel;
        }

        public RelayCommand NavigateToGameCommand { get; }
        public RelayCommand NavigateToHistoryCommand { get; }
        public RelayCommand NavigateToProfileCommand { get; }
        public RelayCommand NavigateToAuthCommand { get; }

        public AuthViewModel AuthViewModel { get; }
        public GameViewModel GameViewModel { get; }
        public HistoryViewModel HistoryViewModel { get; }
        public ProfileViewModel ProfileViewModel { get; }

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        // Обработчики событий от AuthViewModel
        private void OnAuthLoginSuccess(User user)
        {
            CurrentUser = user;
            HistoryViewModel.SetCurrentUser(user); // Устанавливаем текущего пользователя для HistoryViewModel
            
            // Start a new game for the logged-in user
            if (_gameSessionService != null)
            {
                _ = Task.Run(async () => await _gameSessionService.StartNewGameAsync(user));
            }
            
            CurrentView = GameViewModel; // Переход к экрану игры
        }

        private void OnAuthRegistrationSuccess()
        {
            // Можно просто обновить сообщение в AuthViewModel или перейти к Login
            // AuthViewModel.Message = "Регистрация успешна! Теперь войдите.";
            // Или сразу переключить режим
            AuthViewModel.IsRegisterMode = false; // Переключаем на экран входа
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Методы для навигации (если нужно вызывать из других VM или Views)
        //public void NavigateToAuth() => CurrentView = AuthViewModel;
        //public void NavigateToGame() => CurrentView = GameViewModel;
        //public void NavigateToHistory() => CurrentView = HistoryViewModel;
        //public void NavigateToProfile() => CurrentView = ProfileViewModel;
    }
}
```

### App.xaml.cs
```csharp
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartChess.Data;
using SmartChess.Data.Repository;
using SmartChess.Services;
using SmartChess.ViewModels;
using SmartChess.Views;
using SmartChess.Algorithms;
using System;
using System.Windows;
using System.Windows.Threading;

namespace SmartChess
{
    public partial class App : Application
    {
        private IHost? _host;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureServices((context, services) =>
                {
                    // Конфигурация
                    services.AddSingleton<IConfiguration>(context.Configuration);

                    // База данных
                    services.AddDbContext<AppDbContext>();
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IGameRepository, GameRepository>();
                    services.AddScoped<IMoveRepository, MoveRepository>();
                    services.AddScoped<DatabaseService>();

                    // Сервисы
                    services.AddScoped<AuthService>();
                    services.AddScoped<IChessEngine, ChessEngine>();
                    services.AddScoped<GameSessionService>();
                    services.AddScoped<AlgorithmService>();
                    services.AddScoped<PathFinder>();
                    services.AddScoped<GameAnalyzer>();
                    services.AddScoped<MoveCalculator>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<GameViewModel>();
                    services.AddTransient<HistoryViewModel>();
                    services.AddTransient<ProfileViewModel>();
                    services.AddTransient<AuthViewModel>();


                    // Views
                    services.AddTransient<MainWindow>();
                })
                .Build();

            await EnsureAdminUserExists();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private async Task EnsureAdminUserExists()
        {
            using var scope = _host.Services.CreateScope();
            var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();

            var existingAdmin = await dbService.GetUserByLoginAsync("admin");
            if (existingAdmin == null)
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                var adminUser = new Models.Entities.User
                {
                    Login = "admin",
                    PasswordHash = passwordHash
                };

                await dbService.CreateUserAsync(adminUser);
                await dbService.SaveChangesAsync(); // Сохранить изменения в БД
                Console.WriteLine("Администратор 'admin' создан с паролем 'admin123'.");
            }
            else
            {
                Console.WriteLine("Администратор 'admin' уже существует.");
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
```

### App.xaml
```xml
<Application x:Class="SmartChess.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:local="clr-namespace:SmartChess"
             mc:Ignorable="d"
             Startup="Application_Startup">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/GlobalStyles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### Views/MainWindow.xaml
```xml
<Window x:Class="SmartChess.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:SmartChess.Views"
        xmlns:vm="clr-namespace:SmartChess.ViewModels"
        xmlns:uc="clr-namespace:SmartChess.Views.UserControls"
        mc:Ignorable="d"
        Title="SmartChess" Height="600" Width="800">

    <Window.Resources>
        <!-- DataTemplate для AuthViewModel -->
        <DataTemplate DataType="{x:Type vm:AuthViewModel}">
            <uc:LoginView />
        </DataTemplate>
        <!-- DataTemplate для GameViewModel -->
        <DataTemplate DataType="{x:Type vm:GameViewModel}">
            <uc:ChessBoardView />
        </DataTemplate>
        <!-- DataTemplate для HistoryViewModel -->
        <DataTemplate DataType="{x:Type vm:HistoryViewModel}">
            <uc:GameHistoryView />
        </DataTemplate>
        <!-- DataTemplate для ProfileViewModel -->
        <DataTemplate DataType="{x:Type vm:ProfileViewModel}">
            <uc:UserProfileView />
        </DataTemplate>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <!-- Панель навигации -->
            <RowDefinition Height="*"/>
            <!-- Контент -->
        </Grid.RowDefinitions>

        <!-- Панель навигации -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Background="LightGray">
            <Button Content="Игра" Command="{Binding NavigateToGameCommand}" Margin="5" Padding="10,5"/>
            <Button Content="История" Command="{Binding NavigateToHistoryCommand}" Margin="5" Padding="10,5"/>
            <Button Content="Профиль" Command="{Binding NavigateToProfileCommand}" Margin="5" Padding="10,5"/>
            <Button Content="Выход" Command="{Binding NavigateToAuthCommand}" Margin="5" Padding="10,5"/>
        </StackPanel>

        <!-- Контент (шахматы, история, профиль) -->
        <ContentControl Grid.Row="1" Content="{Binding CurrentView}"/>
    </Grid>
</Window>
```

### SmartChess.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <AssemblyName>SmartChess</AssemblyName>
    <RootNamespace>SmartChess</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>

  <ItemGroup>
    <None Remove="Resources\Images\Bishop_B.png" />
    <None Remove="Resources\Images\Bishop_W.png" />
    <None Remove="Resources\Images\King_B.png" />
    <None Remove="Resources\Images\King_W.png" />
    <None Remove="Resources\Images\Knight_B.png" />
    <None Remove="Resources\Images\Knight_W.png" />
    <None Remove="Resources\Images\Pawn_B.png" />
    <None Remove="Resources\Images\Pawn_W.png" />
    <None Remove="Resources\Images\Queen_B.png" />
    <None Remove="Resources\Images\Queen_W.png" />
    <None Remove="Resources\Images\Rook_B.png" />
    <None Remove="Resources\Images\Rook_W.png" />
  </ItemGroup>

  <ItemGroup>
    <Resource Include="Resources\Images\Bishop_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Bishop_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\King_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\King_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Knight_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Knight_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Pawn_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Pawn_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Queen_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Queen_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Rook_B.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
    <Resource Include="Resources\Images\Rook_W.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Resource>
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Data\Migrations\" />
  </ItemGroup>

</Project>
```