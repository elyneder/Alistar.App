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
    public string DesejaServir { get; set; } = string.Empty;
    public VidaPessoal Entrevista_Vida_Pessoal { get; set; } = new VidaPessoal();
    public Arrimo Entrevista_Arrimo_De_Familia { get; set; } = new Arrimo();
    public Cursos Entrevista_Cursos { get; set; } = new Cursos();
    public Experiencia Entrevista_Experiencia { get; set; } = new Experiencia();
    public Habilitacao Entrevista_Habilitacao { get; set; } = new Habilitacao();
    public PreImediata Entrevista_Pre_Qualificacao_Imediata { get; set; } = new PreImediata();
    public Esportes Entrevista_Esportes { get; set; } = new Esportes();
    public Lazer Entrevista_Lazer { get; set; } = new Lazer();
    public Saude Entrevista_Saude { get; set; } = new Saude();
    public Infracao Entrevista_Infracao { get; set; } = new Infracao();
    public EntrevistaMedica Entrevista_Medica { get; set; } = new EntrevistaMedica();
}
