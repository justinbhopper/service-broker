using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface ITableChangeHandler
    {
        Task HandleAsync(IEnumerable<TableChange> changes);
    }
}
