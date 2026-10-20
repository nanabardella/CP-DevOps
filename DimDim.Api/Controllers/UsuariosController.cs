using Asp.Versioning;
using DimDim.Api.DTOs;
using DimDim.Application;
using Microsoft.AspNetCore.Mvc;
namespace DimDim.Api.Controllers;
[ApiController, ApiVersionNeutral, Route("api/usuarios")]
public class UsuariosController(UsuarioService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UsuarioResponse>>> Get(CancellationToken ct) => Ok(await service.GetAll(ct));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> GetById(int id, CancellationToken ct) => Ok(await service.GetById(id, ct));
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Post(UsuarioRequest request, CancellationToken ct)
    {
        var item = await service.Create(request, ct); return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, UsuarioRequest request, CancellationToken ct) { await service.Update(id, request, ct); return NoContent(); }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await service.Delete(id, ct); return NoContent(); }
}
