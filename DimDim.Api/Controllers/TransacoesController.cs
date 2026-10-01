using DimDim.Api.Data;
using DimDim.Api.DTOs;
using DimDim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace DimDim.Api.Controllers;

[ApiController]
[Route("api/transacoes")]
public class TransacoesController(AppDbContext db) : ControllerBase
{
    private IQueryable<TransacaoResponse> Responses => db.Transacoes.AsNoTracking().OrderBy(x => x.Id)
        .Select(x => new TransacaoResponse(x.Id, x.Descricao, x.Valor, x.Data, x.UsuarioId,
            new UsuarioResponse(x.Usuario!.Id, x.Usuario.Nome, x.Usuario.Email)));
    [HttpGet]
    public async Task<ActionResult<List<TransacaoResponse>>> Get(CancellationToken ct) => await Responses.ToListAsync(ct);
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransacaoResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await Responses.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }
    [HttpPost]
    public async Task<ActionResult<TransacaoResponse>> Post(TransacaoRequest request, CancellationToken ct)
    {
        if (!await db.Usuarios.AnyAsync(x => x.Id == request.UsuarioId, ct))
            return BadRequest(new { mensagem = "UsuarioId não existe." });
        var item = new Transacao { Descricao = request.Descricao.Trim(), Valor = request.Valor, Data = request.Data, UsuarioId = request.UsuarioId };
        db.Transacoes.Add(item);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        { return BadRequest(new { mensagem = "UsuarioId não existe mais." }); }
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, await Responses.SingleAsync(x => x.Id == item.Id, ct));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, TransacaoRequest request, CancellationToken ct)
    {
        var item = await db.Transacoes.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (!await db.Usuarios.AnyAsync(x => x.Id == request.UsuarioId, ct))
            return BadRequest(new { mensagem = "UsuarioId não existe." });
        item.Descricao = request.Descricao.Trim(); item.Valor = request.Valor;
        item.Data = request.Data; item.UsuarioId = request.UsuarioId;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        { return BadRequest(new { mensagem = "UsuarioId não existe mais." }); }
        return NoContent();
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Transacoes.FindAsync([id], ct);
        if (item is null) return NotFound();
        db.Transacoes.Remove(item); await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

