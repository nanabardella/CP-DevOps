using DimDim.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DimDim.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("Usuarios");
        usuario.HasKey(x => x.Id);
        usuario.Property(x => x.Nome).IsRequired().HasMaxLength(100);
        usuario.Property(x => x.Email).IsRequired().HasMaxLength(150);
        usuario.HasIndex(x => x.Email).IsUnique();
        var transacao = modelBuilder.Entity<Transacao>();
        transacao.ToTable("Transacoes");
        transacao.HasKey(x => x.Id);
        transacao.Property(x => x.Descricao).IsRequired().HasMaxLength(200);
        transacao.Property(x => x.Valor).HasPrecision(18, 2);
        transacao.HasOne(x => x.Usuario).WithMany(x => x.Transacoes)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
