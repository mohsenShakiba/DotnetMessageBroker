namespace MessageBroker.Common.Pooling
{
    public interface IObjectPool
    {
        T Rent<T>() where T : new();
        void Return<T>(T o);
    }
}