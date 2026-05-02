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
    public VidaPessoal Entrevista_Vida_Pessoal { get; set; }
    public Arrimo Entrevista_Arrimo_De_Familia { get; set; }
    public Cursos Entrevista_Cursos { get; set; }
    public Experiencia Entrevista_Experiencia { get; set; }
    public Habilitacao Entrevista_Habilitacao { get; set; }
    public PreImediata Entrevista_Pre_Qualificacao_Imediata { get; set; }
    public Esportes Entrevista_Esportes { get; set; }
    public Lazer Entrevista_Lazer { get; set; }
    public Saude Entrevista_Saude { get; set; }
    public Infracao Entrevista_Infracao { get; set; }
    public EntrevistaMedica Entrevista_Medica { get; set; }
}
