using System.Collections.Generic;
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

        public async Task HandleAsync(IEnumerable<TableChange> tableChanges)
        {
            foreach (var change in tableChanges)
            {
                await HandleAsync(change);
            }
        }

        private async Task HandleAsync(TableChange tableChange)
        {
            var synchronizer = _synchronizerMapper.GetSynchronizer(tableChange.TableName);

            if (tableChange.Inserted?.Count > 0)
                await synchronizer.InsertAsync(tableChange.Inserted);

            if (tableChange.Updated?.Count > 0)
                await synchronizer.InsertAsync(tableChange.Updated);

            if (tableChange.Deleted?.Count > 0)
                await synchronizer.DeleteAsync(tableChange.Deleted);
        }
    }
}
