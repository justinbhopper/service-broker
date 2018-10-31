using System;
using System.Data;
using System.Data.SqlClient;
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
        
        public void Listen(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () => await LoopAsync(cancellationToken), TaskCreationOptions.LongRunning);
        }
        
        private async Task LoopAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                await CheckForMessageAsync();
            }
        }

        private async Task CheckForMessageAsync()
        {
            try
            {
                var message = await _sqlConnection.QueryFirstAsync<Message>(_command);
                
                if (message != null)
                {
                    await _messageHandler.HandleAsync(message);
                }
            }
            catch (Exception e)
            {
                // TODO: add handleerror to IQueueMessageHandler
            }
        }
    }
}
