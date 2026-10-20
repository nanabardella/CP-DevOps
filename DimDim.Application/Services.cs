using DimDim.Api.DTOs;
using DimDim.Api.Models;
using DimDim.Domain;
namespace DimDim.Application;
public class UsuarioService(IRepository<Usuario> repository)
{
    public static UsuarioResponse Map(Usuario u) => new(u.Id, u.Nome, u.Email);
    public async Task<List<UsuarioResponse>> GetAll(CancellationToken ct) => (await repository.GetAll(ct)).Select(Map).ToList();
    public async Task<UsuarioResponse> GetById(int id, CancellationToken ct) => Map(await Find(id, ct));
    private async Task<Usuario> Find(int id, CancellationToken ct) => await repository.GetById(id, ct) ?? throw new ResourceNotFoundException("Usuário não encontrado.");
    public async Task<UsuarioResponse> Create(UsuarioRequest request, CancellationToken ct)
    {
        var entity = new Usuario { Nome = request.Nome.Trim(), Email = request.Email.Trim() };
        await repository.Add(entity, ct); await repository.Save(ct); return Map(entity);
    }
    public async Task Update(int id, UsuarioRequest request, CancellationToken ct)
    {
        var entity = await Find(id, ct); entity.Nome = request.Nome.Trim(); entity.Email = request.Email.Trim(); await repository.Save(ct);
    }
    public async Task Delete(int id, CancellationToken ct) { repository.Remove(await Find(id, ct)); await repository.Save(ct); }
}
public class TransacaoService(IRepository<Transacao> repository, IRepository<Usuario> usuarios)
{
    private static TransacaoResponse Map(Transacao t) => new(t.Id, t.Descricao, t.Valor, t.Data, t.UsuarioId, UsuarioService.Map(t.Usuario!));
    public async Task<List<TransacaoResponse>> GetAll(CancellationToken ct) => (await repository.GetAll(ct)).Select(Map).ToList();
    public async Task<PagedResult<TransacaoResponse>> GetPaged(int page, int pageSize, CancellationToken ct)
    {
        var request = new PageRequest(page, pageSize); var result = await repository.GetPaged(request, ct);
        return PagedResult<TransacaoResponse>.Create(request, result.TotalItems, result.Items.Select(Map).ToList());
    }
    private async Task<Transacao> Find(int id, CancellationToken ct) => await repository.GetById(id, ct) ?? throw new ResourceNotFoundException("Transação não encontrada.");
    public async Task<TransacaoResponse> GetById(int id, CancellationToken ct) => Map(await Find(id, ct));
    private async Task<Usuario> FindUsuario(int id, CancellationToken ct) => await usuarios.GetById(id, ct) ?? throw new DomainException("UsuarioId não existe.");
    public async Task<TransacaoResponse> Create(TransacaoRequest request, CancellationToken ct)
    {
        var usuario = await FindUsuario(request.UsuarioId, ct);
        var entity = new Transacao { Descricao = request.Descricao.Trim(), Valor = request.Valor, Data = request.Data, UsuarioId = request.UsuarioId, Usuario = usuario };
        await repository.Add(entity, ct); await repository.Save(ct); return Map(entity);
    }
    public async Task Update(int id, TransacaoRequest request, CancellationToken ct)
    {
        var entity = await Find(id, ct); entity.Usuario = await FindUsuario(request.UsuarioId, ct);
        entity.Descricao = request.Descricao.Trim(); entity.Valor = request.Valor; entity.Data = request.Data; entity.UsuarioId = request.UsuarioId; await repository.Save(ct);
    }
    public async Task Delete(int id, CancellationToken ct) { repository.Remove(await Find(id, ct)); await repository.Save(ct); }
}
