using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace ServiceBroker
{
    public class QueueMessageListener : IQueueMessageListener
    {
        private readonly SqlConnection _sqlConnection;
        private readonly IQueueMessageHandler _messageHandler;
        private readonly CommandDefinition _command;

        public QueueMessageListener(SqlConnection sqlConnection, IQueueMessageHandler messageHandler)
        {
            _sqlConnection = sqlConnection;
            _messageHandler = messageHandler;
            _command = new CommandDefinition("dbo.spx_ReceiveSyncRequest", new { timeoutMs = 100 }, commandTimeout: 2, commandType: CommandType.StoredProcedure);
        }
        
        public void Listen(CancellationToken cancellationToken = default(CancellationToken))
        {
            Task.Factory.StartNew(async () => await LoopUntilAsync(cancellationToken), TaskCreationOptions.LongRunning);
        }
        
        private async Task LoopUntilAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                await CheckForMessagesAsync();
            }
        }

        private async Task CheckForMessagesAsync()
        {
            try
            {
                var messages = await _sqlConnection.QueryAsync<Message>(_command);

                int count = messages.Count();

                if (count > 0)
                {
                    await _messageHandler.HandleAsync(messages);
                }
            }
            catch (Exception e)
            {
                // TODO: add handleerror to IQueueMessageHandler
            }
        }
    }
}
