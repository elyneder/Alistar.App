using Alistar.App.Models;
using Alistar.App.Services;
using System.Windows;
using System.Windows.Controls;

namespace Alistar.App;

/// <summary>
/// Tela modal usada para cadastrar novos entrevistadores.
/// </summary>
/// <remarks>
/// A regra de permissao fica no ServicoAutenticacao: somente o administrador
/// pode cadastrar outras contas.
/// </remarks>
public partial class TelaCadastroUsuario : Window
{
    // Indica se a senha esta sendo exibida como texto ou protegida no PasswordBox.
    private bool _senhaVisivel;
    private readonly bool _retornarParaTelaAnterior;
    private ContaUsuario _entrevistador;

    public TelaCadastroUsuario(bool retornarParaTelaAnterior = false)
    {
        _retornarParaTelaAnterior = retornarParaTelaAnterior;
        InitializeComponent();
    }

    /// <summary>
    /// Alterna a visualizacao da senha sem perder o valor digitado.
    /// </summary>
    private void AlternarSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_senhaVisivel)
        {
            CaixaSenhaUsuario.Password = CaixaTextoSenhaVisivelUsuario.Text;
            CaixaSenhaUsuario.Visibility = Visibility.Visible;
            BordaSenhaVisivelUsuario.Visibility = Visibility.Collapsed;
            IconeOlhoAberto.Visibility = Visibility.Visible;
            IconeOlhoFechado.Visibility = Visibility.Collapsed;
        }
        else
        {
            CaixaTextoSenhaVisivelUsuario.Text = CaixaSenhaUsuario.Password;
            CaixaSenhaUsuario.Visibility = Visibility.Collapsed;
            BordaSenhaVisivelUsuario.Visibility = Visibility.Visible;
            IconeOlhoAberto.Visibility = Visibility.Collapsed;
            IconeOlhoFechado.Visibility = Visibility.Visible;
        }

        _senhaVisivel = !_senhaVisivel;
    }

    /// <summary>
    /// Valida os campos e solicita o cadastro ao servico de autenticacao.
    /// </summary>
    private void SalvarCadastroBotao_Click(object sender, RoutedEventArgs e)
    {
        var nome = CaixaTextoNomeUsuario.Text.Trim();
        var email = CaixaTextoEmailUsuario.Text.Trim();
        var CRM = CaixaCRMMedico.Text.Trim();
        var senha = _senhaVisivel ? CaixaTextoSenhaVisivelUsuario.Text.Trim() : CaixaSenhaUsuario.Password.Trim();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            TextoFeedbackCadastro.Text = "Não foi possível concluir o registro";
            return;
        }

        if (_entrevistador.Medico == true && string.IsNullOrWhiteSpace(CRM))
        {
            TextoFeedbackCadastro.Text = "Adicione o CRM para avançar";
            return;
        }

        if(_entrevistador.Medico == false)
        {
            CRM = string.Empty;
        }

        var resultadoCadastro = ServicoAutenticacao.Cadastrar(nome, email, senha, _entrevistador.Medico, _entrevistador.Entrevistador, _entrevistador.AdministradorGeral, CRM);

        if (resultadoCadastro == ResultadoCadastroUsuario.EmailJaCadastrado)
        {
            TextoFeedbackCadastro.Text = "Já existe uma conta com este e-mail.";
            return;
        }

        MessageBox.Show("Entrevistador cadastrado com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        VoltarParaOrigemOuPainel();
    }

    /// <summary>
    /// Fecha a janela sem salvar cadastro.
    /// </summary>
    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        VoltarParaOrigemOuPainel();
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

    private void ClickAdministrador(object sender, RoutedEventArgs e)
    {
        _entrevistador = new ContaUsuario { 
            AdministradorGeral = true,
            Medico = false, 
            Entrevistador = false, 
        };

        ENTREVISTADOR.Style = (Style)FindResource("BotaoPrimarioStyle");
        MEDICO.Style = (Style)FindResource("BotaoPrimarioStyle");
        ADM.Style = (Style)FindResource("BotaoSecundarioStyle");

        BordaCRMMedico.Visibility = Visibility.Collapsed;
        CRMTexto.Visibility = Visibility.Collapsed;
        CaixaCRMMedico.Visibility = Visibility.Collapsed;
    }

    private void ClickEntrevistador(object sender, RoutedEventArgs e)
    {
        _entrevistador = new ContaUsuario
        {
            AdministradorGeral = false,
            Medico = false,
            Entrevistador = true
        };

        ADM.Style = (Style)FindResource("BotaoPrimarioStyle");
        MEDICO.Style = (Style)FindResource("BotaoPrimarioStyle");
        ENTREVISTADOR.Style = (Style)FindResource("BotaoSecundarioStyle");

        BordaCRMMedico.Visibility = Visibility.Collapsed;
        CRMTexto.Visibility = Visibility.Collapsed;
        CaixaCRMMedico.Visibility = Visibility.Collapsed;
    }

    private void ClickMedico(object sender, RoutedEventArgs e)
    {
        _entrevistador = new ContaUsuario
        {
            AdministradorGeral = false,
            Medico = true,
            Entrevistador = false
        };

        ENTREVISTADOR.Style = (Style)FindResource("BotaoPrimarioStyle");
        ADM.Style = (Style)FindResource("BotaoPrimarioStyle");
        MEDICO.Style = (Style)FindResource("BotaoSecundarioStyle");

        BordaCRMMedico.Visibility = Visibility.Visible;
        CRMTexto.Visibility = Visibility.Visible;
        CaixaCRMMedico.Visibility = Visibility.Visible;
    }
}
