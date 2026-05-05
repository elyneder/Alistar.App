namespace Alistar.App.Models;

/// <summary>
/// Modelo da avaliacao medica da segunda e quarta etapa.
/// </summary>
/// <remarks>
/// A segunda etapa cria a ficha medica inicial. A quarta etapa reaproveita estes
/// mesmos campos para reavaliar e atualizar a pessoa. Tudo aqui fica separado da
/// ficha geral porque sao dados mais especificos da area medica.
/// </remarks>
public class EntrevistaMedica
{
    // Avaliacao fisica.
    public string Altura { get; set; } = string.Empty;
    public string Peso { get; set; } = string.Empty;
    public string ProblemaPostura { get; set; } = string.Empty;
    public string ObservacaoProblemaPostura { get; set; } = string.Empty;

    // Visao e audicao.
    public string DificuldadeVisualOuPrecisaOculos { get; set; } = string.Empty;
    public string TesteAuditivoAlterado { get; set; } = string.Empty;
    public string ObservacaoTesteAuditivo { get; set; } = string.Empty;

    // Sinais gerais medidos na avaliacao.
    public string PressaoArterial { get; set; } = string.Empty;
    public string FrequenciaCardiaca { get; set; } = string.Empty;
    public string Respiracao { get; set; } = string.Empty;

    // Historico de saude usado para confirmar aptidao.
    public string FamiliaTemDoencasGraves { get; set; } = string.Empty;
    public string JaTeveProblemaCardiacoOuRespiratorio { get; set; } = string.Empty;
    public string JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico { get; set; } = string.Empty;
    public string TemDificuldadeParaDormir { get; set; } = string.Empty;
}
