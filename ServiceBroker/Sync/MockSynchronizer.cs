using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class MockSynchronizer : ICTOneSynchronizer
    {
        public int Processed;

        public Task InsertOrUpdateAsync(ICollection<int> insertedOrUpdated)
        {
            Processed++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ICollection<int> deleted)
        {
            Processed++;
            return Task.CompletedTask;
        }
    }
}
