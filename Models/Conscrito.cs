using System.Text.Json.Serialization;

namespace Alistar.App.Models;

/// <summary>
/// Modelo principal do sistema. Representa um conscrito e concentra os dados
/// preenchidos nas etapas de alistamento.
/// </summary>
/// <remarks>
/// As propriedades simples guardam a identificacao. As propriedades Entrevista_*
/// apontam para classes de modelo separadas, uma para cada bloco do formulario.
/// </remarks>
public class Conscrito
{
    /// <summary>Identificador interno usado para atualizar ou excluir o registro correto.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nome completo do conscrito. O atributo mantem compatibilidade com JSON antigo.</summary>
    [JsonPropertyName("Name")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Dados de identificacao usados para localizar o conscrito no sistema.</summary>
    public string CPF { get; set; } = string.Empty;
    public string RA { get; set; } = string.Empty;
    public string Situacao { get; set; } = "Indefinido";

    /// <summary>Informacoes basicas coletadas antes dos blocos da entrevista.</summary>
    [JsonPropertyName("MotherName")]
    public string NomeMae { get; set; } = string.Empty;

    [JsonPropertyName("BirthDate")]
    public string DataNascimento { get; set; } = string.Empty;

    public string PaisResidencia { get; set; } = string.Empty;
    public string MunicipioResidencia { get; set; } = string.Empty;
    public string ZonaResidencia { get; set; } = string.Empty;
    public string DesejaServir { get; set; } = string.Empty;

    /// <summary>Blocos da entrevista da primeira etapa.</summary>
    public VidaPessoal Entrevista_Vida_Pessoal { get; set; } = new();
    public Arrimo Entrevista_Arrimo_De_Familia { get; set; } = new();
    public Cursos Entrevista_Cursos { get; set; } = new();
    public Experiencia Entrevista_Experiencia { get; set; } = new();
    public Habilitacao Entrevista_Habilitacao { get; set; } = new();
    public PreImediata Entrevista_Pre_Qualificacao_Imediata { get; set; } = new();
    public Esportes Entrevista_Esportes { get; set; } = new();
    public Lazer Entrevista_Lazer { get; set; } = new();
    public Saude Entrevista_Saude { get; set; } = new();
    public Infracao Entrevista_Infracao { get; set; } = new();

    /// <summary>Bloco da segunda etapa, voltada para avaliacao medica.</summary>
    public EntrevistaMedica Entrevista_Medica { get; set; } = new();
}
