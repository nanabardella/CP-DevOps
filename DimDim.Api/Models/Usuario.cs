namespace DimDim.Api.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<Transacao> Transacoes { get; set; } = new();
}