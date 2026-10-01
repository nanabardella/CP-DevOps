using DimDim.Api.Data;
using DimDim.Api.DTOs;
using DimDim.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace DimDim.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UsuarioResponse>>> Get(CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new UsuarioResponse(x.Id, x.Nome, x.Email)).ToListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await db.Usuarios.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new UsuarioResponse(x.Id, x.Nome, x.Email)).FirstOrDefaultAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Post(UsuarioRequest request, CancellationToken ct)
    {
        var item = new Usuario { Nome = request.Nome.Trim(), Email = request.Email.Trim() };
        db.Usuarios.Add(item);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict(new { mensagem = "Email já cadastrado." }); }
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new UsuarioResponse(item.Id, item.Nome, item.Email));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, UsuarioRequest request, CancellationToken ct)
    {
        var item = await db.Usuarios.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Nome = request.Nome.Trim();
        item.Email = request.Email.Trim();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict(new { mensagem = "Email já cadastrado." }); }
        return NoContent();
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Usuarios.FindAsync([id], ct);
        if (item is null) return NotFound();
        db.Usuarios.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

