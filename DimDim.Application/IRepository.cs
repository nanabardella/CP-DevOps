namespace DimDim.Application;
public interface IRepository<T> where T : class
{
    Task<List<T>> GetAll(CancellationToken ct = default);
    Task<T?> GetById(int id, CancellationToken ct = default);
    Task<PagedResult<T>> GetPaged(PageRequest request, CancellationToken ct = default);
    Task Add(T entity, CancellationToken ct = default);
    void Remove(T entity);
    Task Save(CancellationToken ct = default);
}
