namespace Alistar.App.Models;

/// <summary>
/// Guarda respostas sobre historico de infracoes.
/// </summary>
public class Infracao
{
    public string JaFoiDetidoPelaPolicia { get; set; } = string.Empty;
    public string QualFoiAInfracao { get; set; } = string.Empty;
    public string OutrosAtosInfracionais { get; set; } = string.Empty;
}
