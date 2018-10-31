using System.Collections.Generic;

namespace ServiceBroker
{
    public class TableChangeMessage
    {
        public string TableName { get; set; }
        public IList<TableChangeMessageItem> InsertedOrUpdated { get; set; }
        public IList<TableChangeMessageItem> Deleted { get; set; }
    }
}
