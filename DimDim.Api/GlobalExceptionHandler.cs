using DimDim.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace DimDim.Api;
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch { ArgumentException or DomainException => 400, ResourceNotFoundException => 404, ConflictException => 409, _ => 500 };
        logger.LogError(exception, "Falha HTTP {Status}; traceId={TraceId}", status, context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, Exception = exception, ProblemDetails = new ProblemDetails {
            Status = status, Title = status == 500 ? "Erro interno." : exception.Message, Extensions = { ["traceId"] = context.TraceIdentifier }
        }});
    }
}
