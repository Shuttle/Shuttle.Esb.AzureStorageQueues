namespace Shuttle.Esb.AzureMQ
{
    public interface IAzureMQConfiguration
    {
        string GetConnectionString(string name);
    }
}