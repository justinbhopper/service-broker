using System.Collections.Generic;

namespace ServiceBroker
{
    public class TableChange
    {
        private IList<int> _insertedOrUpdated;
        private IList<int> _deleted;

        public TableChange(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }

        public IList<int> InsertedOrUpdated
        {
            get => _insertedOrUpdated ?? (_insertedOrUpdated = new List<int>());
            set => _insertedOrUpdated = value;
        }

        public IList<int> Deleted
        {
            get => _deleted ?? (_deleted = new List<int>());
            set => _deleted = value;
        }
    }
}
