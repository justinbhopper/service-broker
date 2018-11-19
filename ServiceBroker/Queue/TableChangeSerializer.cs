using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ServiceBroker
{
    public class TableChangeSerializer
    {
        private readonly JsonSerializer _jsonSerializer;

        public TableChangeSerializer()
            : this(JsonSerializer.CreateDefault()) { }

        public TableChangeSerializer(JsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public IEnumerable<TableChange> Deserialize(byte[] message, Encoding encoding)
        {
            IList<TableChangeMessage> changeMessages;
            using (var stream = new MemoryStream(message))
            using (var reader = new StreamReader(stream, encoding))
            {
                changeMessages = _jsonSerializer.Deserialize(reader, typeof(IList<TableChangeMessage>)) as IList<TableChangeMessage>;
            }

            // Don't bother with unnecessary overhead if just one message
            if (changeMessages.Count == 1)
            {
                var changeMessage = changeMessages[0];
                return new[]
                {
                    new TableChange(changeMessage.TableName)
                    {
                        Inserted = changeMessage.Inserted?.Select(i => i.Id).ToList(),
                        Updated = changeMessage.Updated?.Select(i => i.Id).ToList(),
                        Deleted = changeMessage.Deleted?.Select(i => i.Id).ToList()
                    }
                };
            }

            // Multiple messages, so we must merge the ones with the same table name
            var changes = new Dictionary<string, TableChange>();
            foreach (var changeMessage in changeMessages)
            {
                if (changeMessage.Inserted == null && changeMessage.Updated == null && changeMessage.Deleted == null)
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

                if (changeMessage.Inserted != null)
                    tableChange.Inserted.AddRange(changeMessage.Inserted.Select(i => i.Id));

                if (changeMessage.Updated != null)
                    tableChange.Updated.AddRange(changeMessage.Updated.Select(i => i.Id));

                if (changeMessage.Deleted != null)
                    tableChange.Deleted.AddRange(changeMessage.Deleted.Select(i => i.Id));
            }

            return changes.Values;
        }
    }
}
