namespace PreflightApi.Domain.Entities
{
    public interface INasrEntity<T> where T : class
    {
        string CreateUniqueKey();
        void UpdateFrom(T source, HashSet<string>? limitToProperties = null);
        T CreateSelectiveEntity(HashSet<string> properties);
    }

}
