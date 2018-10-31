using System.Threading.Tasks;

namespace ServiceBroker
{
    public class TableChangeHandler : ITableChangeHandler
    {
        private readonly ICTOneSynchronizerMapper _synchronizerMapper;

        public TableChangeHandler(ICTOneSynchronizerMapper synchronizerMapper)
        {
            _synchronizerMapper = synchronizerMapper;
        }

        public async Task HandleAsync(TableChange tableChange)
        {
            var synchronizer = _synchronizerMapper.GetSynchronizer(tableChange.TableName);

            if (tableChange.InsertedOrUpdated?.Count > 0)
                await synchronizer.InsertOrUpdateAsync(tableChange.InsertedOrUpdated);

            if (tableChange.Deleted?.Count > 0)
                await synchronizer.DeleteAsync(tableChange.Deleted);
        }
    }
}
