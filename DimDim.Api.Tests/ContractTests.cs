using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DimDim.Api.Data;
using DimDim.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
public class ContractTests
{
    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Development").UseSetting("ConnectionStrings:DefaultConnection", "test-only");
            builder.ConfigureServices(services => {
                services.RemoveAll<AppDbContext>(); services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
            });
        }
        public HttpClient SeededClient()
        {
            var client = CreateClient();
            using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            var usuario = new Usuario { Nome="Teste",Email="teste@example.com" }; db.Usuarios.Add(usuario);
            for(int i=1;i<=5;i++) db.Transacoes.Add(new Transacao { Descricao="Compra "+i,Valor=i,Data=new DateTime(2026,10,20),Usuario=usuario });
            db.SaveChanges(); return client;
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if(disposing) connection.Dispose(); }
    }
    [Fact]
    public async Task VersionsAndPaginationKeepContracts()
    {
        using var factory=new ApiFactory(); using var client=factory.SeededClient();
        var v1=await client.GetAsync("/api/transacoes?api-version=1.0"); Assert.Equal(HttpStatusCode.OK,v1.StatusCode);
        Assert.Equal(JsonValueKind.Array,JsonDocument.Parse(await v1.Content.ReadAsStringAsync()).RootElement.ValueKind);
        Assert.Contains("2.0",v1.Headers.GetValues("api-supported-versions")); Assert.Contains("1.0",v1.Headers.GetValues("api-deprecated-versions"));
        using var headerRequest=new HttpRequestMessage(HttpMethod.Get,"/api/transacoes"); headerRequest.Headers.Add("X-Api-Version","1.0");
        var byHeader=await client.SendAsync(headerRequest); Assert.Equal(await v1.Content.ReadAsStringAsync(),await byHeader.Content.ReadAsStringAsync());
        var first=JsonDocument.Parse(await client.GetStringAsync("/api/transacoes?pageSize=2"));
        var second=JsonDocument.Parse(await client.GetStringAsync("/api/transacoes?api-version=2.0&page=2&pageSize=2"));
        Assert.Equal(5,first.RootElement.GetProperty("totalItems").GetInt32()); Assert.Equal(3,first.RootElement.GetProperty("totalPages").GetInt32());
        var ids1=first.RootElement.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetInt32()).ToArray();
        var ids2=second.RootElement.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetInt32()).ToArray();
        Assert.Equal(2,ids1.Length); Assert.Equal(2,ids2.Length); Assert.Empty(ids1.Intersect(ids2));
        var huge=JsonDocument.Parse(await client.GetStringAsync("/api/transacoes?page=2147483647&pageSize=100")); Assert.Equal(0,huge.RootElement.GetProperty("items").GetArrayLength());
        foreach(var query in new[]{"page=0","pageSize=9999","pageSize=0"}) Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/transacoes?"+query)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/usuarios")).StatusCode);
        var swagger1=await client.GetStringAsync("/swagger/v1.0/swagger.json"); var swagger2=await client.GetStringAsync("/swagger/v2.0/swagger.json");
        Assert.Contains("DEPRECADA",swagger1); Assert.Contains("/api/usuarios",swagger2); Assert.Contains("/api/transacoes",swagger2);
    }
    [Fact]
    public async Task RateLimitReturnsRetryAfterButHealthAndGetRemainAvailable()
    {
        using var factory=new ApiFactory(); using var client=factory.SeededClient();
        HttpResponseMessage? last=null;
        for(int i=0;i<11;i++) last=await client.PostAsJsonAsync("/api/transacoes",new {descricao="Rate",valor=1,data="2026-10-20T12:00:00",usuarioId=1});
        Assert.Equal(HttpStatusCode.TooManyRequests,last!.StatusCode); Assert.NotNull(last.Headers.RetryAfter);
        Assert.Equal(429,JsonDocument.Parse(await last.Content.ReadAsStringAsync()).RootElement.GetProperty("status").GetInt32());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/transacoes")).StatusCode);
    }
    [Fact]
    public async Task CrudAndCascadeRemainAvailableWithDefaultVersion()
    {
        using var factory=new ApiFactory(); using var client=factory.SeededClient();
        var create=await client.PostAsJsonAsync("/api/usuarios",new {nome="Nova",email="nova@example.com"}); Assert.Equal(HttpStatusCode.Created,create.StatusCode);
        var user=JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
        var tx=await client.PostAsJsonAsync("/api/transacoes",new {descricao="Nova",valor=5,data="2026-10-20T12:00:00",usuarioId=user}); Assert.Equal(HttpStatusCode.Created,tx.StatusCode);
        var id=JsonDocument.Parse(await tx.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent,(await client.PutAsJsonAsync("/api/transacoes/"+id,new {descricao="Atualizada",valor=6,data="2026-10-20T12:00:00",usuarioId=user})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync("/api/usuarios/"+user)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/transacoes/"+id)).StatusCode);
    }
}
