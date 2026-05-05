namespace Alistar.App.Models;

/// <summary>
/// Dados sobre CNH e curso de habilitacao.
/// </summary>
public class Habilitacao
{
    /// <summary>Indica se o conscrito ja possui carteira de motorista.</summary>
    public string PossuiCNH { get; set; } = string.Empty;

    /// <summary>Indica se ele esta fazendo curso para tirar habilitacao.</summary>
    public string RealizandoCursoParaHabilitacao { get; set; } = string.Empty;

    /// <summary>Categoria da CNH, como A, B ou AB.</summary>
    public string CategoriaCNH { get; set; } = string.Empty;
}
