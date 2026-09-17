using System.Threading.Channels;

namespace LudoGame.Server.Services
{
    public class GameCommandQueue
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

        public GameCommandQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }

        public async ValueTask QueueCommandAsync(Func<CancellationToken, ValueTask> command)
        {
            await _queue.Writer.WriteAsync(command);
        }

        public IAsyncEnumerable<Func<CancellationToken, ValueTask>> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
