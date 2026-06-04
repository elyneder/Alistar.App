using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Lista os entrevistadores cadastrados para consulta do administrador.
/// </summary>
public partial class TelaEntrevistadores : Window
{
    private readonly bool _retornarParaTelaAnterior;

    public TelaEntrevistadores(bool retornarParaTelaAnterior = false)
    {
        _retornarParaTelaAnterior = retornarParaTelaAnterior;
        InitializeComponent();

        if (_retornarParaTelaAnterior)
        {
            BotaoTelaInicial.Content = " ⚙️ Voltar para configuração";
            BotaoCadastrarEntrevistador.Visibility = Visibility.Collapsed;
            BotaoCadastrarEntrevistador.IsEnabled = false;
        }

        CarregarEntrevistadores();
    }

    private void CarregarEntrevistadores()
    {
        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            MessageBox.Show("Apenas administradores podem ver os usuários cadastrados.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        var entrevistadores = ServicoAutenticacao.ObterEntrevistadores();
        GradeEntrevistadores.ItemsSource = entrevistadores;
        TextoResumoEntrevistadores.Text = $"{entrevistadores.Count} usuário(s) cadastrado(s).";
        TextoTotalEntrevistadores.Text = entrevistadores.Count.ToString();
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        VoltarParaOrigemOuPainel();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaCadastroUsuario());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
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

        CarregarEntrevistadores();
    }

    private static bool TentarObterEntrevistadorDoBotao(object sender, out EntrevistadorResumo entrevistador)
    {
        entrevistador = (sender as FrameworkElement)?.DataContext as EntrevistadorResumo ?? new();
        return !string.IsNullOrWhiteSpace(entrevistador.Email);
    }

    private void AbrirEdicaoEntrevistador(EntrevistadorResumo entrevistador)
    {
        DialogoEntrevistador.MostrarEdicao(this, entrevistador, CarregarEntrevistadores);
    }

    private void MostrarDetalhesEntrevistador(EntrevistadorResumo entrevistador)
    {
        DialogoEntrevistador.MostrarDetalhes(this, entrevistador);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        RestaurarTelaAnteriorSeNecessario();
    }

    private void VoltarParaOrigemOuPainel()
    {
        if (_retornarParaTelaAnterior && Owner is Window telaAnterior)
        {
            telaAnterior.Show();
            telaAnterior.Activate();
            Close();
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void RestaurarTelaAnteriorSeNecessario()
    {
        if (!_retornarParaTelaAnterior || Owner is not Window telaAnterior || telaAnterior.IsVisible)
        {
            return;
        }

        if (Application.Current.Windows.OfType<TelaLogin>().Any(janela => janela.IsVisible))
        {
            return;
        }

        telaAnterior.Show();
        telaAnterior.Activate();
    }
}
