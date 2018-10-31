using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ServiceBroker
{
    public class TableChangeSerializer
    {
        private readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault();
        
        public IEnumerable<TableChange> Deserialize(byte[] message, Encoding encoding)
        {
            IList<TableChangeMessage> changeMessages;
            using (var stream = new MemoryStream(message))
            using (var reader = new StreamReader(stream, encoding))
            {
                changeMessages = _jsonSerializer.Deserialize(reader, typeof(IList<TableChangeMessage>)) as IList<TableChangeMessage>;
            }

            var changes = new Dictionary<string, TableChange>();
            foreach (var changeMessage in changeMessages)
            {
                if (changeMessage.InsertedOrUpdated == null && changeMessage.Deleted == null)
                    continue;

                string tableName = changeMessage.TableName;

                TableChange tableChange;
                if (!changes.ContainsKey(tableName))
                {
                    tableChange = new TableChange(tableName);
                    changes[tableName] = tableChange;
                }
                else
                {
                    tableChange = changes[tableName];
                }

                if (changeMessage.InsertedOrUpdated != null)
                    tableChange.InsertedOrUpdated = changeMessage.InsertedOrUpdated.Select(i => i.Id).ToList();

                if (changeMessage.Deleted != null)
                    tableChange.Deleted = changeMessage.Deleted.Select(i => i.Id).ToList();
            }

            return changes.Values;
        }
    }
}
