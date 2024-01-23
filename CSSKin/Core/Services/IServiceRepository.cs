namespace CSSKin.Core.Services;

public interface  IServiceRepository<T>
{
    T Create(T data);
    void Delete(string uuid);
    IEnumerable<T?> Get();
    IEnumerable<T>? Get(string uuid);
    void Update(T data);
}