namespace DimDim.Api.Models;

public class Transacao
{
    public int Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime Data { get; set; }

    public int UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }
}