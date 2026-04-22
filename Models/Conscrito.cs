using System.Text.Json.Serialization;

namespace Alistar.App.Models;

public class Conscrito
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Nome { get; set; } = string.Empty;

    public string CPF { get; set; } = string.Empty;
    public string RA { get; set; } = string.Empty;
    public string Situacao { get; set; } = "Indefinido";

    [JsonPropertyName("MotherName")]
    public string NomeMae { get; set; } = string.Empty;

    [JsonPropertyName("BirthDate")]
    public string DataNascimento { get; set; } = string.Empty;

    public string PaisResidencia { get; set; } = string.Empty;
    public string MunicipioResidencia { get; set; } = string.Empty;
    public string ZonaResidencia { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public string Endereco { get; set; } = string.Empty;

    public string Bairro { get; set; } = string.Empty;
    public string CEP { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    [JsonPropertyName("City")]
    public string Municipio { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Ocupacao { get; set; } = string.Empty;
    public string MoraCom { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string PossuiFilhos { get; set; } = string.Empty;
    public string QuantidadeFilhos { get; set; } = string.Empty;
    public string QuemTrabalhaNaFamilia { get; set; } = string.Empty;
    public string QuemSustentaAFamilia { get; set; } = string.Empty;
    public string RecebeAuxilioGovernamental { get; set; } = string.Empty;
    public string SituacaoArrimo { get; set; } = string.Empty;
    public string EstudaAtualmente { get; set; } = string.Empty;

    [JsonPropertyName("Education")]
    public string AnoQueEstaCursandoOuUltimoAnoQueCursou { get; set; } = string.Empty;

    public string TemCursosProfissionalizantes { get; set; } = string.Empty;
    public string QuaisCursosProfissionalizantes { get; set; } = string.Empty;
    public string ComprovaCursosProfissionalizantes { get; set; } = string.Empty;
    public string ExperienciaProfissional { get; set; } = string.Empty;
    public string QuaisExperienciasProfissionais { get; set; } = string.Empty;
    public string ComprovaExperienciaProfissional { get; set; } = string.Empty;
    public string PossuiCNH { get; set; } = string.Empty;
    public string RealizandoCursoParaHabilitacao { get; set; } = string.Empty;
    public string CategoriaCNH { get; set; } = string.Empty;
    public string PrimeiroPadraoPreQualificacao { get; set; } = string.Empty;
    public string SegundoPadraoPreQualificacao { get; set; } = string.Empty;
    public string PraticaEsportes { get; set; } = string.Empty;
    public string QuaisEsportes { get; set; } = string.Empty;
    public string EhOuJaFoiFederado { get; set; } = string.Empty;
    public string SabeNadar { get; set; } = string.Empty;
    public string OQueFazNasHorasDeLazer { get; set; } = string.Empty;
    public string JaTeveProblemaSaude { get; set; } = string.Empty;
    public string QualProblemaSaude { get; set; } = string.Empty;
    public string UsaRemedioControlado { get; set; } = string.Empty;
    public string QualRemedioControlado { get; set; } = string.Empty;
    public string ParaQueUsaRemedioControlado { get; set; } = string.Empty;
    public string HaQuantoTempoUsaRemedioControlado { get; set; } = string.Empty;
    public string PorQuantoTempoAindaUsaraRemedio { get; set; } = string.Empty;
    public string JaEsteveInternadoHospitalOuClinicaPsiquiatrica { get; set; } = string.Empty;
    public string MotivoInternacao { get; set; } = string.Empty;
    public string TempoInternacao { get; set; } = string.Empty;
    public string Fuma { get; set; } = string.Empty;
    public string HaQuantoTempoFuma { get; set; } = string.Empty;
    public string FazUsoBebidaAlcoolica { get; set; } = string.Empty;
    public string FrequenciaBebidaAlcoolica { get; set; } = string.Empty;
    public string JaExperimentouDrogas { get; set; } = string.Empty;
    public string QualDroga { get; set; } = string.Empty;
    public string AindaFazUsoDroga { get; set; } = string.Empty;
    public string FrequenciaUsoDroga { get; set; } = string.Empty;
    public string QuandoFoiUltimaVezQueUtilizouDroga { get; set; } = string.Empty;
    public string PossuiParenteUsuarioDrogas { get; set; } = string.Empty;
    public string QuemParenteUsuarioDrogas { get; set; } = string.Empty;
    public string ComoParenteUsuarioDrogasAfetaSuaVida { get; set; } = string.Empty;
    public string PossuiParenteComHistoricoTranstornoPsiquiatrico { get; set; } = string.Empty;
    public string QuemParenteComHistoricoTranstornoPsiquiatrico { get; set; } = string.Empty;
    public string ComoTranstornoPsiquiatricoAfetaSuaVida { get; set; } = string.Empty;
    public string JaFoiDetidoPelaPolicia { get; set; } = string.Empty;
    public string QualFoiAInfracao { get; set; } = string.Empty;
    public string OutrosAtosInfracionais { get; set; } = string.Empty;
    public string DesejaServir { get; set; } = string.Empty;
}
