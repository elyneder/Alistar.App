using System.Text.Json.Serialization;

namespace Alistar.App.Models;

/// <summary>
/// Guarda as respostas sobre arrimo de familia e estudo.
/// </summary>
/// <remarks>
/// Arrimo e quando o conscrito tem responsabilidade forte no sustento da casa.
/// Esse bloco ajuda a entender a situacao familiar antes da classificacao final.
/// </remarks>
public class Arrimo
{
    /// <summary>Informa se o conscrito se declarou arrimo de familia.</summary>
    public string SituacaoArrimo { get; set; } = string.Empty;

    /// <summary>Mostra se ele ainda esta estudando no momento da entrevista.</summary>
    public string EstudaAtualmente { get; set; } = string.Empty;

    /// <summary>Serie atual ou ultimo ano concluido. O atributo mantem compatibilidade com JSON antigo.</summary>
    [JsonPropertyName("Education")]
    public string AnoQueEstaCursandoOuUltimoAnoQueCursou { get; set; } = string.Empty;
}
