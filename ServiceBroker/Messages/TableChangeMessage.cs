using System.Collections.Generic;

namespace ServiceBroker
{
    public class TableChangeMessage
    {
        public string TableName { get; set; }
        public IList<TableChangeMessageItem> Inserted { get; set; }
        public IList<TableChangeMessageItem> Updated { get; set; }
        public IList<TableChangeMessageItem> Deleted { get; set; }
    }
}
