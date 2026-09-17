using Microsoft.AspNetCore.SignalR;
using LudoGame.Server.Hubs;
using LudoGame.Database;
using LudoGame.Database.Entities;
using System.Text.Json;

namespace LudoGame.Server.Services
{
    public class GameEngineWorker : BackgroundService
    {
        private readonly GameCommandQueue _queue;
        private readonly ILogger<GameEngineWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<LudoHub> _hubContext;
        private readonly SnapshotGameLogger _gameLogger;

        public GameManager Manager { get; }

        public GameEngineWorker(
            GameCommandQueue queue,
            ILogger<GameEngineWorker> logger,
            IServiceProvider serviceProvider,
            IHubContext<LudoHub> hubContext)
        {
            _queue = queue;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;

            _gameLogger = new SnapshotGameLogger { DelayMilliseconds = 0 };
            
            // Listen to snapshot changes and push to clients
            _gameLogger.SnapshotChanged += async (snapshot) =>
            {
                await _hubContext.Clients.All.SendAsync("UpdateState", snapshot);
            };

            Manager = new GameManager(
                logger: _gameLogger,
                sectionPresenter: new NullSectionPresenter(),
                observers: Array.Empty<IGameObserver>()
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Game Engine Worker started.");

            await foreach (var command in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await command(stoppingToken);

                    // Save state to DB
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                    
                    var record = new GameStateRecord
                    {
                        Timestamp = DateTime.UtcNow,
                        LatestAction = "Command Processed",
                        SerializedSnapshot = JsonSerializer.Serialize(_gameLogger.GetSnapshot())
                    };
                    db.GameStates.Add(record);
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing game command.");
                }
            }
        }
    }
}
