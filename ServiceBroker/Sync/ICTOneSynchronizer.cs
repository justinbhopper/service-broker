using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface ICTOneSynchronizer
    {
        Task InsertOrUpdateAsync(ICollection<int> insertedOrUpdated);
        Task DeleteAsync(ICollection<int> deleted);
    }
}
