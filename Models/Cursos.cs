namespace Alistar.App.Models;

/// <summary>
/// Representa cursos profissionalizantes informados pelo conscrito.
/// </summary>
/// <remarks>
/// Esse bloco entra como qualificacao: nao decide tudo sozinho, mas ajuda no ranking
/// e na analise do perfil do candidato.
/// </remarks>
public class Cursos
{
    /// <summary>Resposta Sim/Nao sobre possuir algum curso profissionalizante.</summary>
    public string TemCursosProfissionalizantes { get; set; } = string.Empty;

    /// <summary>Campo aberto para listar quais cursos foram feitos.</summary>
    public string QuaisCursosProfissionalizantes { get; set; } = string.Empty;

    /// <summary>Indica se o conscrito consegue comprovar esses cursos.</summary>
    public string ComprovaCursosProfissionalizantes { get; set; } = string.Empty;
}
