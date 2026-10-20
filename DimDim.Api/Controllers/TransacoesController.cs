using Asp.Versioning;
using DimDim.Api.DTOs;
using DimDim.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace DimDim.Api.Controllers;
[ApiController, Route("api/transacoes")]
[ApiVersion("1.0", Deprecated = true), ApiVersion("2.0")]
public class TransacoesController(TransacaoService service) : ControllerBase
{
    [HttpGet, MapToApiVersion("1.0")]
    public async Task<ActionResult<List<TransacaoResponse>>> GetV1(CancellationToken ct) => Ok(await service.GetAll(ct));
    [HttpGet, MapToApiVersion("2.0")]
    public async Task<ActionResult<PagedResult<TransacaoResponse>>> GetV2(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => Ok(await service.GetPaged(page, pageSize, ct));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransacaoResponse>> GetById(int id, CancellationToken ct) => Ok(await service.GetById(id, ct));
    [HttpPost, EnableRateLimiting("escrita")]
    public async Task<ActionResult<TransacaoResponse>> Post(TransacaoRequest request, CancellationToken ct)
    {
        var item = await service.Create(request, ct); return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, TransacaoRequest request, CancellationToken ct) { await service.Update(id, request, ct); return NoContent(); }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await service.Delete(id, ct); return NoContent(); }
}
