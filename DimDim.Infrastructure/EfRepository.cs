using DimDim.Api.Data;
using DimDim.Api.Models;
using DimDim.Application;
using DimDim.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace DimDim.Infrastructure;
public class EfRepository<T>(AppDbContext db) : IRepository<T> where T : class
{
    private IQueryable<T> Query()
    {
        var query = db.Set<T>().AsQueryable();
        if (typeof(T) == typeof(Transacao)) query = query.Include("Usuario");
        return query;
    }
    public Task<List<T>> GetAll(CancellationToken ct = default) => Query().OrderBy(x => EF.Property<int>(x, "Id")).ToListAsync(ct);
    public Task<T?> GetById(int id, CancellationToken ct = default) => Query().FirstOrDefaultAsync(x => EF.Property<int>(x, "Id") == id, ct);
    public async Task<PagedResult<T>> GetPaged(PageRequest request, CancellationToken ct = default)
    {
        var query = Query().AsNoTracking(); var count = await query.CountAsync(ct);
        // Offset longo protege page=int.MaxValue. A página é cortada no SQL.
        var items = request.Offset >= count ? new List<T>() : await query.OrderBy(x => EF.Property<int>(x, "Id")).Skip((int)request.Offset).Take(request.PageSize).ToListAsync(ct);
        return PagedResult<T>.Create(request, count, items);
    }
    public async Task Add(T entity, CancellationToken ct = default) => await db.Set<T>().AddAsync(entity, ct);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
    public async Task Save(CancellationToken ct = default)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) { throw new ConflictException("Email já cadastrado."); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 547 }) { throw new DomainException("UsuarioId não existe mais."); }
    }
}
