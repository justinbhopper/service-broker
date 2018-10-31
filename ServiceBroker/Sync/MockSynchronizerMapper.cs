namespace ServiceBroker
{
    public class MockSynchronizerMapper : ICTOneSynchronizerMapper
    {
        private readonly ICTOneSynchronizer _synchronizer;

        public MockSynchronizerMapper(ICTOneSynchronizer synchronizer)
        {
            _synchronizer = synchronizer;
        }

        public ICTOneSynchronizer GetSynchronizer(string tableName)
        {
            return _synchronizer;
        }
    }
}
