using System.Text.Json.Serialization;

namespace Alistar.App.Models;

/// <summary>
/// Modelo principal do sistema. Representa um conscrito e concentra os dados
/// preenchidos nas etapas de alistamento.
/// </summary>
/// <remarks>
/// Esta classe e usada pelo servico de armazenamento para gravar e ler os dados
/// no arquivo JSON. As propriedades ficam como string porque a tela recebe os
/// valores diretamente dos campos do formulario.
/// </remarks>
public class Conscrito
{
    /// <summary>
    /// Identificador interno usado para atualizar ou excluir o registro correto.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do conscrito. O atributo mantem compatibilidade com JSON antigo.
    /// </summary>
    [JsonPropertyName("Name")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Dados de identificacao usados para localizar o conscrito no sistema.
    /// </summary>
    public string CPF { get; set; } = string.Empty;
    public string RA { get; set; } = string.Empty;
    public string Situacao { get; set; } = "Indefinido";

    /// <summary>
    /// Informacoes basicas coletadas na primeira etapa.
    /// </summary>
    [JsonPropertyName("MotherName")]
    public string NomeMae { get; set; } = string.Empty;

    [JsonPropertyName("BirthDate")]
    public string DataNascimento { get; set; } = string.Empty;
    public string PaisResidencia { get; set; } = string.Empty;
    public string MunicipioResidencia { get; set; } = string.Empty;
    public string ZonaResidencia { get; set; } = string.Empty;
    public string DesejaServir { get; set; } = string.Empty;

    /// <summary>
    /// Campos preenchidos na segunda etapa, voltada para avaliacao medica.
    /// </summary>
    public string Altura { get; set; } = string.Empty;
    public string Peso { get; set; } = string.Empty;
    public string ProblemaPostura { get; set; } = string.Empty;
    public string ObservacaoProblemaPostura { get; set; } = string.Empty;
    public string DificuldadeVisualOuPrecisaOculos { get; set; } = string.Empty;
    public string TesteAuditivoAlterado { get; set; } = string.Empty;
    public string ObservacaoTesteAuditivo { get; set; } = string.Empty;
    public string PressaoArterial { get; set; } = string.Empty;
    public string FrequenciaCardiaca { get; set; } = string.Empty;
    public string Respiracao { get; set; } = string.Empty;
    public string FamiliaTemDoencasGraves { get; set; } = string.Empty;
    public string JaTeveProblemaCardiacoOuRespiratorio { get; set; } = string.Empty;
    public string JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico { get; set; } = string.Empty;
    public string TemDificuldadeParaDormir { get; set; } = string.Empty;
}
