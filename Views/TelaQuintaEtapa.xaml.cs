using System.Windows;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Quinta etapa: classifica os conscritos elegiveis para TG e substitutos.
/// </summary>
/// <remarks>
/// Esta tela e tipo a "peneira final": pega todos os conscritos salvos, remove quem
/// nao entra no ranking e calcula uma pontuacao simples para ordenar os melhores.
/// </remarks>
public partial class TelaQuintaEtapa : Window
{
    // Quantidade principal pedida na regra de negocio: os 50 melhores ficam destacados.
    private const int QuantidadeSelecionados = 50;

    public TelaQuintaEtapa()
    {
        InitializeComponent();
        CarregarRanking();
    }

    private void CarregarRanking()
    {
        // Primeiro buscamos todo mundo no JSON. Depois filtramos so TG/Substituto.
        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();
        var elegiveis = conscritos
            .Where(ConscritoEhElegivelParaRanking)
            .Select(CriarItemRanking)
            .OrderByDescending(item => item.Pontuacao)
            .ThenBy(item => PrioridadeSituacao(item.Situacao))
            .ThenBy(item => item.Nome)
            .ToList();

        for (var indice = 0; indice < elegiveis.Count; indice++)
        {
            // Depois da ordenacao, a posicao vira a classificacao visual da tabela.
            elegiveis[indice].Posicao = indice + 1;
            elegiveis[indice].Classificacao = indice < QuantidadeSelecionados
                ? "Top 50"
                : "Substituto";
        }

        var totalTop50 = elegiveis.Count(item => item.Classificacao == "Top 50");
        var totalSubstitutos = elegiveis.Count(item => item.Classificacao == "Substituto");
        var totalExcluidos = conscritos.Count - elegiveis.Count;

        GradeRanking.ItemsSource = elegiveis;
        TextoTotalTop50.Text = totalTop50.ToString();
        TextoTotalSubstitutos.Text = totalSubstitutos.ToString();
        TextoTotalElegiveis.Text = elegiveis.Count.ToString();
        TextoTotalElegiveisLateral.Text = elegiveis.Count.ToString();
        TextoTotalExcluidos.Text = totalExcluidos.ToString();
        TextoFeedbackRanking.Text = elegiveis.Count == 0
            ? "Nenhum conscrito elegível encontrado. Apenas situações TG e Substituto entram nesta etapa."
            : $"Ranking atualizado com {elegiveis.Count} conscrito(s). Inaptos, dispensados, aptos e indefinidos ficaram fora da classificação.";
    }

    private static bool ConscritoEhElegivelParaRanking(Conscrito conscrito)
    {
        var situacao = NormalizarSituacao(conscrito.Situacao);
        return situacao is "TG" or "Substituto";
    }

    private static ItemRanking CriarItemRanking(Conscrito conscrito)
    {
        // A pontuacao junta dados de situacao, cadastro, saude, cursos e experiencia.
        // Nao e uma nota militar oficial; e uma regra simples do sistema para teste e apresentacao.
        var criterios = new List<string>();
        var pontuacao = 0;
        var situacao = NormalizarSituacao(conscrito.Situacao);

        if (situacao == "TG")
        {
            pontuacao += 30;
            criterios.Add("TG");
        }
        else
        {
            pontuacao += 18;
            criterios.Add("Substituto");
        }

        pontuacao += PontuarFichaBasica(conscrito, criterios);
        pontuacao += PontuarFichaMedica(conscrito.Entrevista_Medica, criterios);
        pontuacao += PontuarQualificacoes(conscrito, criterios);
        pontuacao -= PontuarRiscosSaude(conscrito, criterios);

        return new ItemRanking
        {
            Nome = conscrito.Nome,
            RA = conscrito.RA,
            Situacao = situacao,
            Pontuacao = Math.Max(0, pontuacao),
            StatusFicha = FichaMedicaEstaPreenchida(conscrito.Entrevista_Medica)
                ? "Médica completa"
                : "Médica pendente",
            Criterios = string.Join(" | ", criterios)
        };
    }

    private static int PontuarFichaBasica(Conscrito conscrito, List<string> criterios)
    {
        var camposPreenchidos = ContarPreenchidos(
            conscrito.Nome,
            conscrito.CPF,
            conscrito.RA,
            conscrito.NomeMae,
            conscrito.DataNascimento,
            conscrito.PaisResidencia,
            conscrito.MunicipioResidencia,
            conscrito.ZonaResidencia,
            conscrito.DesejaServir,
            conscrito.Entrevista_Vida_Pessoal?.Telefone,
            conscrito.Entrevista_Vida_Pessoal?.Email,
            conscrito.Entrevista_Vida_Pessoal?.Ocupacao);

        var pontos = Math.Min(20, camposPreenchidos * 2);
        if (pontos >= 16)
        {
            criterios.Add("cadastro completo");
        }

        return pontos;
    }

    private static int PontuarFichaMedica(EntrevistaMedica? entrevistaMedica, List<string> criterios)
    {
        if (entrevistaMedica is null)
        {
            return 0;
        }

        var camposPreenchidos = ContarPreenchidos(
            entrevistaMedica.Altura,
            entrevistaMedica.Peso,
            entrevistaMedica.ProblemaPostura,
            entrevistaMedica.DificuldadeVisualOuPrecisaOculos,
            entrevistaMedica.TesteAuditivoAlterado,
            entrevistaMedica.PressaoArterial,
            entrevistaMedica.FrequenciaCardiaca,
            entrevistaMedica.Respiracao,
            entrevistaMedica.FamiliaTemDoencasGraves,
            entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio,
            entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico,
            entrevistaMedica.TemDificuldadeParaDormir);

        var pontos = Math.Min(28, camposPreenchidos * 3);
        if (FichaMedicaEstaPreenchida(entrevistaMedica))
        {
            criterios.Add("exames médicos confirmados");
        }

        return pontos;
    }

    private static int PontuarQualificacoes(Conscrito conscrito, List<string> criterios)
    {
        var pontos = 0;

        if (RespostaEhSim(conscrito.Entrevista_Cursos?.TemCursosProfissionalizantes))
        {
            pontos += RespostaEhSim(conscrito.Entrevista_Cursos?.ComprovaCursosProfissionalizantes) ? 8 : 5;
            criterios.Add("curso");
        }

        if (RespostaEhSim(conscrito.Entrevista_Experiencia?.ExperienciaProfissional))
        {
            pontos += RespostaEhSim(conscrito.Entrevista_Experiencia?.ComprovaExperienciaProfissional) ? 8 : 5;
            criterios.Add("experiência");
        }

        if (RespostaEhSim(conscrito.Entrevista_Habilitacao?.PossuiCNH) ||
            !string.IsNullOrWhiteSpace(conscrito.Entrevista_Habilitacao?.CategoriaCNH))
        {
            pontos += 7;
            criterios.Add("CNH");
        }

        if (RespostaEhSim(conscrito.Entrevista_Esportes?.PraticaEsportes))
        {
            pontos += 5;
            criterios.Add("esporte");
        }

        if (RespostaEhSim(conscrito.Entrevista_Esportes?.SabeNadar))
        {
            pontos += 4;
            criterios.Add("natação");
        }

        return pontos;
    }

    private static int PontuarRiscosSaude(Conscrito conscrito, List<string> criterios)
    {
        var pontos = 0;

        if (RespostaEhSim(conscrito.Entrevista_Saude?.JaTeveProblemaSaude))
        {
            pontos += 8;
            criterios.Add("atenção saúde");
        }

        if (RespostaEhSim(conscrito.Entrevista_Saude?.UsaRemedioControlado))
        {
            pontos += 6;
            criterios.Add("remédio controlado");
        }

        if (RespostaEhSim(conscrito.Entrevista_Saude?.JaEsteveInternadoHospitalOuClinicaPsiquiatrica))
        {
            pontos += 8;
            criterios.Add("histórico internação");
        }

        if (RespostaEhSim(conscrito.Entrevista_Medica?.JaTeveProblemaCardiacoOuRespiratorio))
        {
            pontos += 7;
            criterios.Add("cardiorrespiratório");
        }

        if (RespostaEhSim(conscrito.Entrevista_Medica?.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico))
        {
            pontos += 5;
            criterios.Add("saúde mental");
        }

        return pontos;
    }

    private static bool FichaMedicaEstaPreenchida(EntrevistaMedica? entrevistaMedica)
    {
        return entrevistaMedica is not null &&
               ContarPreenchidos(
                   entrevistaMedica.Altura,
                   entrevistaMedica.Peso,
                   entrevistaMedica.ProblemaPostura,
                   entrevistaMedica.DificuldadeVisualOuPrecisaOculos,
                   entrevistaMedica.TesteAuditivoAlterado,
                   entrevistaMedica.PressaoArterial,
                   entrevistaMedica.FrequenciaCardiaca,
                   entrevistaMedica.Respiracao,
                   entrevistaMedica.FamiliaTemDoencasGraves,
                   entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio,
                   entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico,
                   entrevistaMedica.TemDificuldadeParaDormir) >= 10;
    }

    private static int ContarPreenchidos(params string?[] valores)
    {
        return valores.Count(valor => !string.IsNullOrWhiteSpace(valor) &&
                                      !string.Equals(valor.Trim(), "Selecione", StringComparison.OrdinalIgnoreCase));
    }

    private static bool RespostaEhSim(string? valor)
    {
        return string.Equals(valor?.Trim(), "Sim", StringComparison.OrdinalIgnoreCase);
    }

    private static int PrioridadeSituacao(string situacao)
    {
        return situacao == "TG" ? 0 : 1;
    }

    private static string NormalizarSituacao(string? situacao)
    {
        return string.IsNullOrWhiteSpace(situacao) ? "Indefinido" : situacao.Trim();
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }

    private sealed class ItemRanking
    {
        public int Posicao { get; set; }
        public string Classificacao { get; set; } = string.Empty;
        public string Situacao { get; set; } = string.Empty;
        public string RA { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int Pontuacao { get; set; }
        public string StatusFicha { get; set; } = string.Empty;
        public string Criterios { get; set; } = string.Empty;
    }
}
