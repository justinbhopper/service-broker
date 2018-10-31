using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface ITableChangeHandler
    {
        Task HandleAsync(TableChange tableChange);
    }
}
