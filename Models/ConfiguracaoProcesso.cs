namespace Alistar.App.Models;

/// <summary>
/// Configuração geral do processo seletivo antes da abertura das etapas.
/// </summary>
public class ConfiguracaoProcesso
{
    public DateTime DataAbertura { get; set; } = DateTime.Today;

    public int AnoLimiteNascimento { get; set; } = DateTime.Today.Year - 18;

    public int TotalClassificados { get; set; } = 50;

    public List<string> ServidoresAutorizados { get; set; } = [];

    public List<ConfiguracaoEtapaProcesso> Etapas { get; set; } =
    [
        new() { Numero = 1, Nome = "Primeira Etapa", PercentualEliminacao = 10 },
        new() { Numero = 2, Nome = "Exames Médicos", PercentualEliminacao = 10 },
        new() { Numero = 3, Nome = "Entrevista Técnica", PercentualEliminacao = 10 },
        new() { Numero = 4, Nome = "Reavaliação Médica", PercentualEliminacao = 10 },
        new() { Numero = 5, Nome = "Designação Final", PercentualEliminacao = 0 }
    ];
}

public class ConfiguracaoEtapaProcesso
{
    public int Numero { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int PercentualEliminacao { get; set; }

    public int QuantidadeEliminados { get; set; }

    public List<string> EntrevistadoresAutorizados { get; set; } = [];
}
