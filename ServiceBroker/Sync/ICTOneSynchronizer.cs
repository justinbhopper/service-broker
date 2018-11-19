using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface ICTOneSynchronizer
    {
        Task InsertAsync(ICollection<int> inserted);
        Task UpdateAsync(ICollection<int> updated);
        Task DeleteAsync(ICollection<int> deleted);
    }
}
