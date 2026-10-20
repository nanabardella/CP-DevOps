using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DimDim.Api;
using DimDim.Api.Data;
using DimDim.Application;
using DimDim.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ctx => ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(2, 0); options.AssumeDefaultVersionWhenUnspecified = true; options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version"), new HeaderApiVersionReader("X-Api-Version"));
}).AddMvc().AddApiExplorer(options => options.GroupNameFormat = "'v'VVVV");
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(); builder.Services.ConfigureOptions<ConfigureSwagger>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.MigrationsAssembly("DimDim.Api")));
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>)); builder.Services.AddScoped<UsuarioService>(); builder.Services.AddScoped<TransacaoService>();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    // Política por processo, compartilhada: 10 POSTs/minuto, sem fila. GETs ficam livres.
    options.AddFixedWindowLimiter("escrita", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
    options.OnRejected = async (ctx, ct) => {
        var seconds = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
        ctx.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: 429, title: "Limite de 10 requisições por minuto excedido.", extensions: new Dictionary<string, object?> { ["traceId"] = ctx.HttpContext.TraceIdentifier }).ExecuteAsync(ctx.HttpContext);
    };
});
var insights = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(insights)) builder.Services.AddApplicationInsightsTelemetry(options => options.ConnectionString = insights);
var app = builder.Build(); app.UseExceptionHandler(); app.UseRouting(); app.UseRateLimiter();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled")) {
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
    app.UseSwagger(); app.UseSwaggerUI(options => {
        foreach (var version in app.Services.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions)
            options.SwaggerEndpoint($"/swagger/{version.GroupName}/swagger.json", version.GroupName + (version.IsDeprecated ? " (DEPRECADA)" : ""));
    });
}
app.Use(async (context, next) => {
    using var scope = app.Logger.BeginScope(new Dictionary<string, object> { ["traceId"] = context.TraceIdentifier });
    if (context.Request.Path.StartsWithSegments("/api") && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection"))) {
        await Results.Problem("Configure ConnectionStrings:DefaultConnection para usar o CRUD.", statusCode: 503).ExecuteAsync(context); return;
    }
    await next(context);
    app.Logger.LogInformation("HTTP {Method} {Path} => {Status}; traceId={TraceId}", context.Request.Method, context.Request.Path, context.Response.StatusCode, context.TraceIdentifier);
});
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok", database = "reachable" })
        : Results.Problem(statusCode: 503, title: "Banco de dados indisponível.")).DisableRateLimiting();
app.MapControllers(); app.Run();
public partial class Program;
