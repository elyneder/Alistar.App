using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaPainelControle : Window
{
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

    private void AbrirJanelaEtapa(Window janela, Window janelaAntiga)
    {
        janela.Show();
        CarregarConscritos();
        janelaAntiga?.Close();
    }


    private void CarregarConscritos()
    {
        _conscritosCarregados = ServicoArmazenamentoConscritos.ObterTodos()
            .OrderBy(conscrito => conscrito.Nome)
            .ToList();

        TextoQuantidadeConscritos.Text = _conscritosCarregados.Count.ToString();
        AplicarFiltrosLista();
    }

    private void MostrarVisaoInicial()
    {
        VisaoInicial.Visibility = Visibility.Visible;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
    }

    private void MostrarListaConscritos()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Visible;
        GradeConscritos.SelectedItem = null;
        CarregarConscritos();
    }

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
