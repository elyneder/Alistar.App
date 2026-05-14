namespace Alistar.App.Models;

/// <summary>
/// Registro de auditoria exibido ao administrador geral.
/// </summary>
public class RegistroAuditoria
{
    public DateTime DataHora { get; set; } = DateTime.Now;

    public string UsuarioNome { get; set; } = string.Empty;

    public string UsuarioEmail { get; set; } = string.Empty;

    public string TipoUsuario { get; set; } = string.Empty;

    public string Acao { get; set; } = string.Empty;

    public string Entidade { get; set; } = string.Empty;

    public string Campo { get; set; } = string.Empty;

    public string ValorAnterior { get; set; } = string.Empty;

    public string ValorNovo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;
}
