using System.ComponentModel.DataAnnotations;
namespace DimDim.Api.DTOs;

public class UsuarioRequest
{
    [Required, StringLength(100)] public string Nome { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
}
public record UsuarioResponse(int Id, string Nome, string Email);
public class TransacaoRequest : IValidatableObject
{
    [Required, StringLength(200)] public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    [Range(1, int.MaxValue)] public int UsuarioId { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Data == default) yield return new("Informe uma data válida.", [nameof(Data)]);
        if (Valor < -9999999999999999.99m || Valor > 9999999999999999.99m || decimal.Round(Valor, 2) != Valor)
            yield return new("Valor deve caber em decimal(18,2), com até duas casas decimais.", [nameof(Valor)]);
    }
}
public record TransacaoResponse(int Id, string Descricao, decimal Valor, DateTime Data, int UsuarioId, UsuarioResponse Usuario);

