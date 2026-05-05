namespace Alistar.App.Models;

/// <summary>
/// Guarda os padroes de pre-qualificacao imediata.
/// </summary>
/// <remarks>
/// Sao campos usados para registrar pontos fortes observados antes da decisao final.
/// </remarks>
public class PreImediata
{
    public string PrimeiroPadraoPreQualificacao { get; set; } = string.Empty;
    public string SegundoPadraoPreQualificacao { get; set; } = string.Empty;
}
