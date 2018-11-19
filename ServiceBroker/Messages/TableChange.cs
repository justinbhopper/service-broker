using System.Collections.Generic;

namespace ServiceBroker
{
    public class TableChange
    {
        private List<int> _inserted;
        private List<int> _updated;
        private List<int> _deleted;

        public TableChange(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }

        public List<int> Inserted
        {
            get => _inserted ?? (_inserted = new List<int>());
            set => _inserted = value;
        }

        public List<int> Updated
        {
            get => _updated ?? (_updated = new List<int>());
            set => _updated = value;
        }

        public List<int> Deleted
        {
            get => _deleted ?? (_deleted = new List<int>());
            set => _deleted = value;
        }
    }
}
