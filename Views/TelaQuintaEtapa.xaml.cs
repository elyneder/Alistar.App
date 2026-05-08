using System.Windows;
using System.Windows.Controls;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Quinta etapa: classifica os conscritos elegiveis para TG, refratarios e substitutos.
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
        // Primeiro buscamos todo mundo no JSON. Depois filtramos somente TG/Refratario/Substituto.
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
            ? "Nenhum conscrito TG, Substituto ou Refratario encontrado para a designacao final."
            : $"Ranking atualizado com {elegiveis.Count} conscrito(s). Use o lapis para ajustar a situacao final para TG, Substituto ou Dispensado.";
    }

    private static bool ConscritoEhElegivelParaRanking(Conscrito conscrito)
    {
        var situacao = NormalizarSituacao(conscrito.Situacao);
        return !conscrito.Faltoso &&
               conscrito.PrimeiraEtapaConcluida &&
               conscrito.SegundaEtapaConcluida &&
               conscrito.TerceiraEtapaConcluida &&
               conscrito.QuartaEtapaConcluida &&
               SituacaoApareceNaEtapaFinal(situacao);
    }

    private static ItemRanking CriarItemRanking(Conscrito conscrito)
    {
        // A pontuacao junta dados de situacao, cadastro, saude, cursos e experiencia.
        // Nao e uma nota militar oficial; e uma regra simples do sistema para teste e apresentacao.
        var criterios = new List<string>();
        var pontuacao = 0;
        var situacao = NormalizarSituacao(conscrito.Situacao);

        if (situacao is "TG" or "RefratÃ¡rio")
        {
            pontuacao += 30;
            criterios.Add(situacao);
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
            Id = conscrito.Id,
            Nome = conscrito.Nome,
            RA = conscrito.RA,
            Situacao = situacao,
            Pontuacao = Math.Max(0, pontuacao),
            StatusFicha = FichaMedicaEstaPreenchida(conscrito.Entrevista_Medica)
                ? "MÃ©dica completa"
                : "MÃ©dica pendente",
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
            entrevistaMedica.TipoAnalise,
            entrevistaMedica.CRM,
            entrevistaMedica.ResultadoAptidao,
            entrevistaMedica.Restricao,
            entrevistaMedica.QualProblema,
            entrevistaMedica.MotivoInaptidao,
            entrevistaMedica.CID,
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
            criterios.Add("exames mÃ©dicos confirmados");
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
            criterios.Add("experiÃªncia");
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
            criterios.Add("nataÃ§Ã£o");
        }

        return pontos;
    }

    private static int PontuarRiscosSaude(Conscrito conscrito, List<string> criterios)
    {
        var pontos = 0;

        if (RespostaEhSim(conscrito.Entrevista_Saude?.JaTeveProblemaSaude))
        {
            pontos += 8;
            criterios.Add("atenÃ§Ã£o saÃºde");
        }

        if (RespostaEhSim(conscrito.Entrevista_Saude?.UsaRemedioControlado))
        {
            pontos += 6;
            criterios.Add("remÃ©dio controlado");
        }

        if (RespostaEhSim(conscrito.Entrevista_Saude?.JaEsteveInternadoHospitalOuClinicaPsiquiatrica))
        {
            pontos += 8;
            criterios.Add("histÃ³rico internaÃ§Ã£o");
        }

        if (RespostaEhSim(conscrito.Entrevista_Medica?.JaTeveProblemaCardiacoOuRespiratorio))
        {
            pontos += 7;
            criterios.Add("cardiorrespiratÃ³rio");
        }

        if (RespostaEhSim(conscrito.Entrevista_Medica?.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico))
        {
            pontos += 5;
            criterios.Add("saÃºde mental");
        }

        return pontos;
    }

    private static bool SituacaoApareceNaEtapaFinal(string situacao)
    {
        return situacao is "TG" or "Refratário" or "Substituto";
    }

    private static bool FichaMedicaEstaPreenchida(EntrevistaMedica? entrevistaMedica)
    {
        return entrevistaMedica is not null &&
               ContarPreenchidos(
                   entrevistaMedica.TipoAnalise,
                   entrevistaMedica.CRM,
                   entrevistaMedica.ResultadoAptidao,
                   entrevistaMedica.Restricao,
                   entrevistaMedica.QualProblema,
                   entrevistaMedica.MotivoInaptidao,
                   entrevistaMedica.CID,
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
                   entrevistaMedica.TemDificuldadeParaDormir) >= 3;
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
        return situacao is "TG" or "Refratário" ? 0 : 1;
    }

    private static string NormalizarSituacao(string? situacao)
    {
        if (string.IsNullOrWhiteSpace(situacao))
        {
            return "Indefinido";
        }

        var valor = situacao.Trim();
        return string.Equals(valor, "Refratario", StringComparison.OrdinalIgnoreCase)
            ? "Refratário"
            : valor;
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }

    private void AbrirMenuSituacao_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button botao && botao.ContextMenu is not null)
        {
            AtualizarMenuSituacao(botao);
            botao.ContextMenu.PlacementTarget = botao;
            botao.ContextMenu.IsOpen = true;
        }
    }

    private void AlterarSituacaoTG_Click(object sender, RoutedEventArgs e)
    {
        AlterarSituacaoSelecionada(sender, "TG");
    }

    private void AlterarSituacaoSubstituto_Click(object sender, RoutedEventArgs e)
    {
        AlterarSituacaoSelecionada(sender, "Substituto");
    }

    private void AlterarSituacaoDispensado_Click(object sender, RoutedEventArgs e)
    {
        AlterarSituacaoSelecionada(sender, "Dispensado");
    }

    private void AlterarSituacaoSelecionada(object sender, string novaSituacao)
    {
        if (sender is not MenuItem { Parent: ContextMenu { PlacementTarget: Button botao } } ||
            botao.DataContext is not ItemRanking item)
        {
            return;
        }

        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();
        var conscrito = conscritos.FirstOrDefault(c => c.Id == item.Id);
        if (conscrito is null)
        {
            TextoFeedbackRanking.Text = "Nao foi possivel localizar o conscrito para alterar a situacao.";
            return;
        }

        conscrito.Situacao = novaSituacao;
        ServicoArmazenamentoConscritos.Atualizar(conscrito);
        CarregarRanking();
        TextoFeedbackRanking.Text = $"{conscrito.Nome} atualizado para {novaSituacao}.";
    }

    private static void AtualizarMenuSituacao(Button botao)
    {
        if (botao.ContextMenu is null || botao.DataContext is not ItemRanking item)
        {
            return;
        }

        foreach (var menuItem in botao.ContextMenu.Items.OfType<MenuItem>())
        {
            menuItem.IsEnabled = !string.Equals(
                menuItem.Header?.ToString(),
                item.Situacao,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ItemRanking
    {
        public string Id { get; set; } = string.Empty;
        public int Posicao { get; set; }
        public string Classificacao { get; set; } = string.Empty;
        public string Situacao { get; set; } = string.Empty;
        public string RA { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int Pontuacao { get; set; }
        public string StatusFicha { get; set; } = string.Empty;
        public string Criterios { get; set; } = string.Empty;
    }

    private void GradeRanking_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
}

