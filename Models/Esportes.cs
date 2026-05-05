namespace Alistar.App.Models;

/// <summary>
/// Informacoes esportivas do conscrito.
/// </summary>
/// <remarks>
/// Esses dados ajudam a entender condicionamento, disciplina e habilidades uteis,
/// como saber nadar.
/// </remarks>
public class Esportes
{
    public string PraticaEsportes { get; set; } = string.Empty;
    public string QuaisEsportes { get; set; } = string.Empty;
    public string EhOuJaFoiFederado { get; set; } = string.Empty;
    public string SabeNadar { get; set; } = string.Empty;
}
