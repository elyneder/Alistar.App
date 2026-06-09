using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela principal apos o login. Centraliza acesso as etapas e a lista de conscritos.
/// </summary>
/// <remarks>
/// A tela tambem possui filtros rapidos para consultar cadastros ja salvos.
/// </remarks>
public partial class TelaPainelControle : Window
{
    // Cache local dos conscritos carregados do JSON para pesquisa e filtros.
    private List<Conscrito> _conscritosCarregados = [];
    private List<SituacaoResumo> _resumosSituacao = [];
    private const string FiltroTodasSituacoes = "Todas";

    public TelaPainelControle()
    {
        InitializeComponent();
        CarregarConscritos();
        MostrarVisaoInicial();

        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            VerEntrevistadores.Visibility = Visibility.Collapsed;
            VerEntrevistadores.IsEnabled = false;
            BotaoLogsGerais.Visibility = Visibility.Collapsed;
            BotaoLogsGerais.IsEnabled = false;
            CadastrarEntrevistador.Visibility = Visibility.Collapsed;
            CadastrarEntrevistador.IsEnabled = false;
        }

        AplicarPermissoesEtapas();
        ServicoAuditoria.RegistrarAcao("Acesso", "Painel de Controle", "Usuário abriu o painel de controle.");
    }

    private void PrimeiraEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAuditoria.RegistrarAcao("Acesso", "Primeira Etapa", "Usuário abriu a primeira etapa.");
        if (!ServicoAutenticacao.VerificarSeEhMedico())
        {
            AbrirJanelaEtapa(new TelaPrimeiraEtapa());
        }
        else
        {
            MessageBox.Show("Você não tem permissão para acessar essa tela");
        }
    }

    private void SegundaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAuditoria.RegistrarAcao("Acesso", "Segunda Etapa", "Usuário abriu a segunda etapa.");
        if (!ServicoAutenticacao.VerificarSeEhEntrevistador())
        {
            AbrirJanelaEtapa(new TelaSegundaEtapa());
        }
        else
        {
            MessageBox.Show("Você não tem permissão para acessar essa tela");
        }
    }

    private void TerceiraEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAuditoria.RegistrarAcao("Acesso", "Terceira Etapa", "Usuário abriu a terceira etapa.");
        if (!ServicoAutenticacao.VerificarSeEhMedico())
        {
            ServicoNavegacao.Trocar(this, new TelaPrimeiraEtapa(null, true, true));
        }
        else
        {
            MessageBox.Show("Você não tem permissão para acessar essa Etapa");
        }
    }

    private void QuartaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAuditoria.RegistrarAcao("Acesso", "Quarta Etapa", "Usuário abriu a quarta etapa.");
        if (!ServicoAutenticacao.VerificarSeEhEntrevistador())
        {
            AbrirJanelaEtapa(new TelaSegundaEtapa(modoReavaliacaoMedica: true));
        }
        else
        {
            MessageBox.Show("Você não tem permissão para acessar essa tela");
        }
    }

    private void QuintaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAuditoria.RegistrarAcao("Acesso", "Quinta Etapa", "Usuário abriu a quinta etapa.");
        if (!ServicoAutenticacao.VerificarSeEhMedico() && !ServicoAutenticacao.VerificarSeEhEntrevistador())
        {
            AbrirJanelaEtapa(new TelaQuintaEtapa());
        }
        else
        {
            MessageBox.Show("Você não tem permissão para acessar essa etapa");
        }
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarVisaoInicial();
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarListaConscritos();
    }

    private void MostrarSituacoesBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarSituacoes();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        AbrirJanelaEtapa(new TelaCadastroUsuario());
    }

    private void VerEntrevistadoresBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarEntrevistadores();
    }

    private void MostrarLogsGeraisBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarLogsGerais();
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }

    private void GradeConscritos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GradeConscritos.SelectedItem is not Conscrito conscritoSelecionado)
        {
            return;
        }

        GradeConscritos.SelectedItem = null;
        AbrirJanelaEtapa(new TelaPrimeiraEtapa(conscritoSelecionado, abrirEmContextoListaGeral: true));
    }

    private void CaixaPesquisaConscrito_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        AplicarFiltrosLista();
        e.Handled = true;
    }

    private void PesquisarConscritoBotao_Click(object sender, RoutedEventArgs e)
    {
        AplicarFiltrosLista();
    }

    private void AlternarFiltrosBotao_Click(object sender, RoutedEventArgs e)
    {
        var filtrosVisiveis = PainelFiltrosRapidos.Visibility == Visibility.Visible;
        PainelFiltrosRapidos.Visibility = filtrosVisiveis ? Visibility.Collapsed : Visibility.Visible;
        TextoBotaoFiltros.Text = filtrosVisiveis ? "Filtros" : "Ocultar";
        IconeFiltros.Visibility = filtrosVisiveis ? Visibility.Visible : Visibility.Collapsed;
        IconeOcultar.Visibility = filtrosVisiveis ? Visibility.Collapsed : Visibility.Visible;
    }

    private void FiltroRapido_Changed(object sender, RoutedEventArgs e)
    {
        AplicarFiltrosLista();
    }

    private void LimparFiltrosBotao_Click(object sender, RoutedEventArgs e)
    {
        CaixaPesquisaConscrito.Text = string.Empty;

        FiltroSituacaoTG.IsChecked = false;
        FiltroSituacaoSubstituto.IsChecked = false;
        FiltroSituacaoApto.IsChecked = false;
        FiltroSituacaoInapto.IsChecked = false;
        FiltroSituacaoDispensado.IsChecked = false;
        FiltroSituacaoRefratario.IsChecked = false;
        FiltroSituacaoIndefinido.IsChecked = false;
        FiltroAndamentoEmAndamento.IsChecked = false;
        FiltroAndamentoFinalizado.IsChecked = false;
        FiltroAndamentoFaltoso.IsChecked = false;
        FiltroTrabalha.IsChecked = false;
        FiltroRecebeAuxilio.IsChecked = false;
        FiltroEstuda.IsChecked = false;
        FiltroExperiencia.IsChecked = false;
        FiltroProblemaSaude.IsChecked = false;
        FiltroDesejaServirSim.IsChecked = false;
        FiltroDesejaServirNao.IsChecked = false;

        AplicarFiltrosLista();
    }

    private void ComboFiltroSituacaoResumo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AplicarFiltroSituacoes();
    }

    /// <summary>
    /// Abre uma tela de etapa e fecha a janela anterior para manter um fluxo unico.
    /// </summary>
    private void AbrirJanelaEtapa(Window janela)
    {
        ServicoNavegacao.Trocar(this, janela);
    }

    private void AplicarPermissoesEtapas()
    {
        var email = ServicoAutenticacao.UsuarioAtual?.Email;

        ConfigurarBotaoEtapa(BotaoPrimeiraEtapa, 1, email);
        ConfigurarBotaoEtapa(BotaoSegundaEtapa, 2, email);
        ConfigurarBotaoEtapa(BotaoTerceiraEtapa, 3, email);
        ConfigurarBotaoEtapa(BotaoQuartaEtapa, 4, email);
        ConfigurarBotaoEtapa(BotaoQuintaEtapa, 5, email);
    }

    private static void ConfigurarBotaoEtapa(Button botao, int numeroEtapa, string? email)
    {
        var permitido = ServicoConfiguracaoProcesso.UsuarioPodeAcessarEtapa(numeroEtapa, email);
        botao.IsEnabled = permitido;
        botao.Opacity = permitido ? 1 : 0.45;
        botao.ToolTip = permitido ? null : "Seu usuário não foi autorizado para esta etapa.";
    }


    /// <summary>
    /// Recarrega a lista de conscritos e atualiza o total exibido no menu.
    /// </summary>
    private void CarregarConscritos()
    {
        _conscritosCarregados = ServicoArmazenamentoConscritos.ObterTodos()
            .OrderBy(conscrito => conscrito.Nome)
            .ToList();

        TextoQuantidadeConscritos.Text = _conscritosCarregados.Count.ToString();
        AplicarFiltrosLista();
    }

    /// <summary>
    /// Exibe os cards das etapas e esconde a lista.
    /// </summary>
    private void MostrarVisaoInicial()
    {
        VisaoInicial.Visibility = Visibility.Visible;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
        VisaoSituacoes.Visibility = Visibility.Collapsed;
        VisaoEntrevistadores.Visibility = Visibility.Collapsed;
        VisaoLogsGerais.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Exibe a grade de conscritos cadastrados.
    /// </summary>
    private void MostrarListaConscritos()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Visible;
        VisaoSituacoes.Visibility = Visibility.Collapsed;
        VisaoEntrevistadores.Visibility = Visibility.Collapsed;
        VisaoLogsGerais.Visibility = Visibility.Collapsed;
        GradeConscritos.SelectedItem = null;
        CarregarConscritos();
    }

    private void MostrarSituacoes()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
        VisaoSituacoes.Visibility = Visibility.Visible;
        VisaoEntrevistadores.Visibility = Visibility.Collapsed;
        VisaoLogsGerais.Visibility = Visibility.Collapsed;

        CarregarConscritos();
        AtualizarResumoSituacoes();
        AplicarFiltroSituacoes();
    }

    private void MostrarEntrevistadores()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
        VisaoSituacoes.Visibility = Visibility.Collapsed;
        VisaoEntrevistadores.Visibility = Visibility.Visible;
        VisaoLogsGerais.Visibility = Visibility.Collapsed;

        var entrevistadores = ServicoAutenticacao.ObterEntrevistadores();
        GradeEntrevistadoresPainel.ItemsSource = entrevistadores;
        TextoResumoEntrevistadoresPainel.Text = $"{entrevistadores.Count} usuário(s) cadastrado(s).";
    }

    private void MostrarLogsGerais()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
        VisaoSituacoes.Visibility = Visibility.Collapsed;
        VisaoEntrevistadores.Visibility = Visibility.Collapsed;
        VisaoLogsGerais.Visibility = Visibility.Visible;
        GradeLogsGerais.ItemsSource = ServicoAuditoria.ObterTodos();
    }

    private void DetalhesEntrevistadorBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!TentarObterEntrevistadorDoBotao(sender, out var entrevistador))
        {
            return;
        }

        MostrarDetalhesEntrevistador(entrevistador);
    }

    private void EditarEntrevistadorBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!TentarObterEntrevistadorDoBotao(sender, out var entrevistador))
        {
            return;
        }

        AbrirEdicaoEntrevistador(entrevistador);
    }

    private void ExcluirEntrevistadorBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!TentarObterEntrevistadorDoBotao(sender, out var entrevistador))
        {
            return;
        }

        var resultado = MessageBox.Show(
            $"Você realmente quer excluir o usuário {entrevistador.Nome}?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado != MessageBoxResult.Yes)
        {
            return;
        }

        if (!ServicoAutenticacao.ExcluirEntrevistador(entrevistador.Email))
        {
            MessageBox.Show("Não foi possível excluir este entrevistador.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MostrarEntrevistadores();
    }

    private static bool TentarObterEntrevistadorDoBotao(object sender, out EntrevistadorResumo entrevistador)
    {
        entrevistador = (sender as FrameworkElement)?.DataContext as EntrevistadorResumo ?? new();
        return !string.IsNullOrWhiteSpace(entrevistador.Email);
    }

    private void AbrirEdicaoEntrevistador(EntrevistadorResumo entrevistador)
    {
        DialogoEntrevistador.MostrarEdicao(this, entrevistador, MostrarEntrevistadores);
    }

    private void MostrarDetalhesEntrevistador(EntrevistadorResumo entrevistador)
    {
        DialogoEntrevistador.MostrarDetalhes(this, entrevistador);
    }

    private void AtualizarResumoSituacoes()
    {
        var total = Math.Max(_conscritosCarregados.Count, 1);
        var situacoes = new[] { "TG", "Substituto", "Apto", "Inapto", "Dispensado", "Refratário", "Indefinido" };

        _resumosSituacao = situacoes
            .Select(situacao =>
            {
                var quantidade = _conscritosCarregados.Count(conscrito =>
                    string.Equals(NormalizarSituacao(conscrito.Situacao), situacao, StringComparison.OrdinalIgnoreCase));
                var percentual = Math.Round((double)quantidade / total * 100, 1);

                return new SituacaoResumo
                {
                    Situacao = situacao,
                    Quantidade = quantidade,
                    Percentual = percentual,
                    Cor = ObterCorSituacao(situacao)
                };
            })
            .ToList();

        ListaResumoSituacoes.ItemsSource = _resumosSituacao;

        var filtroAtual = ComboFiltroSituacaoResumo.SelectedItem?.ToString() ?? FiltroTodasSituacoes;
        ComboFiltroSituacaoResumo.ItemsSource = new[] { FiltroTodasSituacoes }.Concat(situacoes).ToList();
        ComboFiltroSituacaoResumo.SelectedItem = ComboFiltroSituacaoResumo.Items.Contains(filtroAtual)
            ? filtroAtual
            : FiltroTodasSituacoes;

        TextoResumoSituacoes.Text = $"{_conscritosCarregados.Count} conscritos cadastrados no total.";
    }

    private void AplicarFiltroSituacoes()
    {
        var filtro = ComboFiltroSituacaoResumo.SelectedItem?.ToString() ?? FiltroTodasSituacoes;
        IEnumerable<SituacaoResumo> resumosGrafico = _resumosSituacao;

        if (!string.Equals(filtro, FiltroTodasSituacoes, StringComparison.OrdinalIgnoreCase))
        {
            resumosGrafico = resumosGrafico.Where(resumo =>
                string.Equals(resumo.Situacao, filtro, StringComparison.OrdinalIgnoreCase));
        }

        var resumosGraficoLista = resumosGrafico.ToList();
        var maiorQuantidade = Math.Max(resumosGraficoLista.MaxBy(resumo => resumo.Quantidade)?.Quantidade ?? 0, 1);
        foreach (var resumo in resumosGraficoLista)
        {
            resumo.AlturaColuna = resumo.Quantidade == 0
                ? 8
                : Math.Max(42, (double)resumo.Quantidade / maiorQuantidade * 230);
        }

        GraficoSituacoes.ItemsSource = resumosGraficoLista;
    }

    private static Brush ObterCorSituacao(string situacao)
    {
        var cor = situacao switch
        {
            "TG" => "#166534",
            "Substituto" => "#22C55E",
            "Apto" => "#FACC15",
            "Inapto" => "#F97316",
            "Dispensado" => "#DC2626",
            "Refratário" => "#7C2D12",
            _ => "#64748B"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(cor)!);
    }

    /// <summary>
    /// Aplica pesquisa textual e filtros marcados na lista de conscritos.
    /// </summary>
    private void AplicarFiltrosLista()
    {
        IEnumerable<Conscrito> consulta = _conscritosCarregados;
        var pesquisa = CaixaPesquisaConscrito.Text.Trim();

        if (!string.IsNullOrWhiteSpace(pesquisa))
        {
            consulta = consulta.Where(conscrito =>
                ContemTexto(conscrito.Nome, pesquisa) ||
                ContemTexto(conscrito.CPF, pesquisa) ||
                ContemTexto(conscrito.RA, pesquisa));
        }

        var situacoesSelecionadas = ObterSituacoesSelecionadas();
        if (situacoesSelecionadas.Count > 0)
        {
            consulta = consulta.Where(conscrito => situacoesSelecionadas.Contains(NormalizarSituacao(conscrito.Situacao)));
        }

        var andamentosSelecionados = ObterAndamentosSelecionados();
        if (andamentosSelecionados.Count > 0)
        {
            consulta = consulta.Where(conscrito => andamentosSelecionados.Contains(conscrito.AndamentoProcesso));
        }

        if (FiltroTrabalha.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => TemTrabalhoDeclarado(conscrito.Entrevista_Vida_Pessoal.Ocupacao));
        }

        if (FiltroRecebeAuxilio.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Vida_Pessoal.RecebeAuxilioGovernamental));
        }

        if (FiltroEstuda.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Arrimo_De_Familia.EstudaAtualmente));
        }

        if (FiltroExperiencia.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Experiencia.ExperienciaProfissional));
        }

        if (FiltroProblemaSaude.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Saude.JaTeveProblemaSaude));
        }

        var filtrosDesejo = new List<string>();
        if (FiltroDesejaServirSim.IsChecked == true)
        {
            filtrosDesejo.Add("Sim");
        }

        if (FiltroDesejaServirNao.IsChecked == true)
        {
            filtrosDesejo.Add("Nao");
        }

        if (filtrosDesejo.Count > 0)
        {
            consulta = consulta.Where(conscrito => filtrosDesejo.Contains(NormalizarResposta(conscrito.DesejaServir)));
        }

        var listaFiltrada = consulta.ToList();
        GradeConscritos.ItemsSource = listaFiltrada;
        TextoResumoLista.Text = $"{listaFiltrada.Count} conscritos encontrados.";
    }

    /// <summary>
    /// Monta o conjunto de situacoes selecionadas nos checkboxes de filtro.
    /// </summary>
    private HashSet<string> ObterSituacoesSelecionadas()
    {
        var situacoes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FiltroSituacaoTG.IsChecked == true) situacoes.Add("TG");
        if (FiltroSituacaoSubstituto.IsChecked == true) situacoes.Add("Substituto");
        if (FiltroSituacaoApto.IsChecked == true) situacoes.Add("Apto");
        if (FiltroSituacaoInapto.IsChecked == true) situacoes.Add("Inapto");
        if (FiltroSituacaoDispensado.IsChecked == true) situacoes.Add("Dispensado");
        if (FiltroSituacaoRefratario.IsChecked == true) situacoes.Add("Refratário");
        if (FiltroSituacaoIndefinido.IsChecked == true) situacoes.Add("Indefinido");

        return situacoes;
    }

    private HashSet<string> ObterAndamentosSelecionados()
    {
        var andamentos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FiltroAndamentoEmAndamento.IsChecked == true) andamentos.Add("Em andamento");
        if (FiltroAndamentoFinalizado.IsChecked == true) andamentos.Add("Finalizado");
        if (FiltroAndamentoFaltoso.IsChecked == true) andamentos.Add("Faltoso");

        return andamentos;
    }

    private static bool ContemTexto(string? valor, string pesquisa)
    {
        return !string.IsNullOrWhiteSpace(valor) &&
               valor.Contains(pesquisa, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RespostaEhSim(string? valor)
    {
        return string.Equals(NormalizarResposta(valor), "Sim", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizarResposta(string? valor)
    {
        return valor?.Trim().Replace("Não", "Nao", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
    }

    private static string NormalizarSituacao(string? situacao)
    {
        return string.IsNullOrWhiteSpace(situacao) ? "Indefinido" : situacao.Trim();
    }

    private static bool TemTrabalhoDeclarado(string? ocupacao)
    {
        if (string.IsNullOrWhiteSpace(ocupacao))
        {
            return false;
        }

        var ocupacaoNormalizada = ocupacao.Trim();
        return !string.Equals(ocupacaoNormalizada, "Nao", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Não", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Nao trabalha", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Não trabalha", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Desempregado", StringComparison.OrdinalIgnoreCase);
    }
}

public class SituacaoResumo
{
    public string Situacao { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public double Percentual { get; set; }

    public Brush Cor { get; set; } = Brushes.SlateGray;

    public string PercentualTexto => $"{Percentual:0.#}%";

    public double AlturaColuna { get; set; }
}
