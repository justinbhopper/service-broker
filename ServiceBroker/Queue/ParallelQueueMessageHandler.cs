using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class ParallelQueueMessageHandler : IQueueMessageHandler
    {
        private readonly Semaphore _semaphore;
        private readonly IQueueMessageHandler _decorated;

        public ParallelQueueMessageHandler(IQueueMessageHandler decorated, int maxParallelProcessing)
        {
            _decorated = decorated;
            _semaphore = new Semaphore(maxParallelProcessing, maxParallelProcessing);
        }
        
        public async Task HandleAsync(Message message)
        {
            _semaphore.WaitOne();

            await Task.Factory.StartNew(async () =>
            {
                await _decorated.HandleAsync(message);
                _semaphore.Release();
            });
        }
    }
}
