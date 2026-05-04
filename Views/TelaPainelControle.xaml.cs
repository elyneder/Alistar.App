using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public TelaPainelControle()
    {
        InitializeComponent();
        CarregarConscritos();
        MostrarVisaoInicial();

        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            CadastrarEntrevistador.Visibility = Visibility.Collapsed;
            CadastrarEntrevistador.IsEnabled = false;
        }
    }

    private void PrimeiraEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaPrimeiraEtapa(), janelaAtual);

    }

    private void SegundaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaSegundaEtapa(), janelaAtual);
    }

    private void TerceiraEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaTerceiraEtapa(), janelaAtual);
    }

    private void QuartaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaQuartaEtapa(), janelaAtual);
    }

    private void QuintaEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaQuintaEtapa(), janelaAtual);
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarVisaoInicial();
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarListaConscritos();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {


        var telaCadastroUsuario = new TelaCadastroUsuario
        {
            Owner = this
        };

        telaCadastroUsuario.ShowDialog();
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.EncerrarSessao();
        var telaLogin = new TelaLogin();
        telaLogin.Show();
        Close();
    }

    private void GradeConscritos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GradeConscritos.SelectedItem is not Conscrito conscritoSelecionado)
        {
            return;
        }

        GradeConscritos.SelectedItem = null;
        Window janelaAtual = Window.GetWindow(sender as DependencyObject);
        AbrirJanelaEtapa(new TelaPrimeiraEtapa(conscritoSelecionado), janelaAtual);
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
        FiltroSituacaoIndefinido.IsChecked = false;
        FiltroTrabalha.IsChecked = false;
        FiltroRecebeAuxilio.IsChecked = false;
        FiltroEstuda.IsChecked = false;
        FiltroExperiencia.IsChecked = false;
        FiltroProblemaSaude.IsChecked = false;
        FiltroDesejaServirSim.IsChecked = false;
        FiltroDesejaServirNao.IsChecked = false;

        AplicarFiltrosLista();
    }

    /// <summary>
    /// Abre uma tela de etapa e fecha a janela anterior para manter um fluxo unico.
    /// </summary>
    private void AbrirJanelaEtapa(Window janela, Window janelaAntiga)
    {
        janela.Show();
        CarregarConscritos();
        janelaAntiga?.Close();
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
    }

    /// <summary>
    /// Exibe a grade de conscritos cadastrados.
    /// </summary>
    private void MostrarListaConscritos()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Visible;
        GradeConscritos.SelectedItem = null;
        CarregarConscritos();
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

        if (FiltroTrabalha.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => TemTrabalhoDeclarado(conscrito.Ocupacao));
        }

        if (FiltroRecebeAuxilio.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.RecebeAuxilioGovernamental));
        }

        if (FiltroEstuda.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.EstudaAtualmente));
        }

        if (FiltroExperiencia.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.ExperienciaProfissional));
        }

        if (FiltroProblemaSaude.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.JaTeveProblemaSaude));
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
        if (FiltroSituacaoIndefinido.IsChecked == true) situacoes.Add("Indefinido");

        return situacoes;
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
