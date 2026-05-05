namespace Alistar.App.Models;

/// <summary>
/// Dados de saude declarados na primeira etapa.
/// </summary>
/// <remarks>
/// Esse bloco nao substitui a avaliacao medica. Ele registra o que o conscrito
/// informa na entrevista geral, e depois a parte medica confirma ou complementa.
/// </remarks>
public class Saude
{
    // Problemas de saude e uso de medicamento.
    public string JaTeveProblemaSaude { get; set; } = string.Empty;
    public string QualProblemaSaude { get; set; } = string.Empty;
    public string UsaRemedioControlado { get; set; } = string.Empty;
    public string QualRemedioControlado { get; set; } = string.Empty;
    public string ParaQueUsaRemedioControlado { get; set; } = string.Empty;
    public string HaQuantoTempoUsaRemedioControlado { get; set; } = string.Empty;
    public string PorQuantoTempoAindaUsaraRemedio { get; set; } = string.Empty;

    // Historico de internacao ou acompanhamento mais serio.
    public string JaEsteveInternadoHospitalOuClinicaPsiquiatrica { get; set; } = string.Empty;
    public string MotivoInternacao { get; set; } = string.Empty;
    public string TempoInternacao { get; set; } = string.Empty;

    // Habitos declarados.
    public string Fuma { get; set; } = string.Empty;
    public string HaQuantoTempoFuma { get; set; } = string.Empty;
    public string FazUsoBebidaAlcoolica { get; set; } = string.Empty;
    public string FrequenciaBebidaAlcoolica { get; set; } = string.Empty;

    // Perguntas sobre drogas e impacto familiar.
    public string JaExperimentouDrogas { get; set; } = string.Empty;
    public string QualDroga { get; set; } = string.Empty;
    public string AindaFazUsoDroga { get; set; } = string.Empty;
    public string FrequenciaUsoDroga { get; set; } = string.Empty;
    public string QuandoFoiUltimaVezQueUtilizouDroga { get; set; } = string.Empty;
    public string PossuiParenteUsuarioDrogas { get; set; } = string.Empty;
    public string QuemParenteUsuarioDrogas { get; set; } = string.Empty;
    public string ComoParenteUsuarioDrogasAfetaSuaVida { get; set; } = string.Empty;

    // Historico psiquiatrico familiar.
    public string PossuiParenteComHistoricoTranstornoPsiquiatrico { get; set; } = string.Empty;
    public string QuemParenteComHistoricoTranstornoPsiquiatrico { get; set; } = string.Empty;
    public string ComoTranstornoPsiquiatricoAfetaSuaVida { get; set; } = string.Empty;
}
