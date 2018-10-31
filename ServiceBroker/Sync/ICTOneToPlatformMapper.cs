namespace ServiceBroker
{
    public interface ICTOneToPlatformMapper<in TCTOneModel, out TPlatformModel>
    {
        TPlatformModel Map(TCTOneModel ctOneModel);
    }
}
