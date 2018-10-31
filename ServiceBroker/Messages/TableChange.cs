using System.Collections.Generic;

namespace ServiceBroker
{
    public class TableChange
    {
        private List<int> _insertedOrUpdated;
        private List<int> _deleted;

        public TableChange(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }

        public List<int> InsertedOrUpdated
        {
            get => _insertedOrUpdated ?? (_insertedOrUpdated = new List<int>());
            set => _insertedOrUpdated = value;
        }

        public List<int> Deleted
        {
            get => _deleted ?? (_deleted = new List<int>());
            set => _deleted = value;
        }
    }
}
