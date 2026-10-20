using DimDim.Application;
using DimDim.Api.Models;
using DimDim.Domain;
using Xunit;
public class PaginationTests
{
    [Theory]
    [InlineData(0,20)] [InlineData(-1,20)] [InlineData(1,0)] [InlineData(1,101)] [InlineData(1,9999)]
    public void RejectsInvalidRange(int page, int size) => Assert.Throws<ArgumentException>(() => new PageRequest(page,size));
    [Fact]
    public void UsesDefaults() { var page = new PageRequest(); Assert.Equal(1,page.Page); Assert.Equal(20,page.PageSize); }
    [Fact]
    public void RoundsTotalPagesUp() { var result = PagedResult<int>.Create(new(2,2),5,[3,4]); Assert.Equal(3,result.TotalPages); Assert.True(result.HasNext); Assert.True(result.HasPrevious); }
    [Fact]
    public void LargePageDoesNotOverflow() => Assert.Equal(214748364600L, new PageRequest(int.MaxValue,100).Offset);
    [Fact]
    public async Task ServiceRejectsBeforeAccessingDatabase()
    {
        var service = new TransacaoService(new UnavailableRepository<Transacao>(), new UnavailableRepository<Usuario>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPaged(0,20,default));
    }
    [Fact]
    public async Task MissingUserIsNotFound()
    {
        var service = new UsuarioService(new UnavailableRepository<Usuario>());
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetById(99,default));
    }
    private class UnavailableRepository<T> : IRepository<T> where T:class
    {
        public Task<List<T>> GetAll(CancellationToken ct=default) => throw new InvalidOperationException();
        public Task<T?> GetById(int id,CancellationToken ct=default) => Task.FromResult<T?>(null);
        public Task<PagedResult<T>> GetPaged(PageRequest page,CancellationToken ct=default) => throw new InvalidOperationException();
        public Task Add(T entity,CancellationToken ct=default) => throw new InvalidOperationException();
        public void Remove(T entity) => throw new InvalidOperationException();
        public Task Save(CancellationToken ct=default) => throw new InvalidOperationException();
    }
}
