using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace ServiceBroker
{
    public class PreTest
    {
        private readonly SqlConnection _connection;
        private readonly CommandDefinition _flushCommand;
        private readonly CommandDefinition _truncateCommand;

        public PreTest(SqlConnection connection)
        {
            _connection = connection;
            _flushCommand = new CommandDefinition("dbo.spx_FlushSyncRequests", commandType: CommandType.StoredProcedure, commandTimeout: 60);
            _truncateCommand = new CommandDefinition("dbo.spx_TruncateClients", commandType: CommandType.StoredProcedure, commandTimeout: 60);
        }
        
        public void Execute()
        {
            _connection.Execute(_flushCommand);
            _connection.Execute(_truncateCommand);
        }
    }
}
