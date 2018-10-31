namespace ServiceBroker
{
    public interface ICTOneSynchronizerMapper
    {
        ICTOneSynchronizer GetSynchronizer(string tableName);
    }
}
