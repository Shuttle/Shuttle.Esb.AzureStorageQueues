namespace Shuttle.Esb.AzureMQ
{
    public interface IAzureStorageConfiguration
    {
        string GetConnectionString(string name);
    }
}