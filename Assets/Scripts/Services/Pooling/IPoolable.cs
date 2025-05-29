namespace Services.Pooling
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}