using Microsoft.AspNetCore.SignalR;
using LudoGame.Server.Services;

namespace LudoGame.Server.Hubs
{
    public class LudoHub : Hub
    {
        private readonly GameCommandQueue _queue;
        private readonly GameEngineWorker _engine;

        public LudoHub(GameCommandQueue queue, GameEngineWorker engine)
        {
            _queue = queue;
            _engine = engine;
        }

        public async Task StartGame()
        {
            await _queue.QueueCommandAsync(async ct =>
            {
                _engine.Manager.StartGame();
                await Task.CompletedTask;
            });
        }

        public async Task PlayTurn()
        {
            await _queue.QueueCommandAsync(async ct =>
            {
                _engine.Manager.PlayNextTurn();
                await Task.CompletedTask;
            });
        }
    }
}
