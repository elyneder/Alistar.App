namespace Alistar.App.Models;

/// <summary>
/// Guarda experiencia profissional declarada na primeira etapa.
/// </summary>
public class Experiencia
{
    /// <summary>Resposta Sim/Nao para experiencia profissional.</summary>
    public string ExperienciaProfissional { get; set; } = string.Empty;

    /// <summary>Descricao simples das experiencias informadas.</summary>
    public string QuaisExperienciasProfissionais { get; set; } = string.Empty;

    /// <summary>Indica se existe comprovacao da experiencia.</summary>
    public string ComprovaExperienciaProfissional { get; set; } = string.Empty;
}
