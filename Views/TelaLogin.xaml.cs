using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela inicial de autenticacao do sistema.
/// </summary>
/// <remarks>
/// Recebe e-mail e senha, valida no ServicoAutenticacao e abre o painel principal
/// quando o login e aprovado.
/// </remarks>
public partial class TelaLogin : Window
{
    // Controla se a senha esta visivel em TextBox ou escondida no PasswordBox.
    private bool _senhaVisivel;

    public TelaLogin()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Evento do botao Entrar. Valida campos obrigatorios e autentica o usuario.
    /// </summary>
    private void EntrarBotao_Click(object sender, RoutedEventArgs e)
    {
        var email = CaixaTextoEmail.Text.Trim();
        var senha = _senhaVisivel ? CaixaTextoSenhaVisivel.Text.Trim() : CaixaSenha.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            TextoFeedback.Text = "Preencha o e-mail e a senha para continuar.";
            return;
        }

        if (!ServicoAutenticacao.ValidarLogin(email, senha))
        {
            TextoFeedback.Text = "E-mail ou senha invalidos.";
            return;
        }

        if (ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            ServicoNavegacao.Trocar(this, new TelaConfiguracaoProcesso());
            return;
        }

        if (!ServicoConfiguracaoProcesso.ProcessoFoiConfigurado())
        {
            ServicoAutenticacao.EncerrarSessao();
            TextoFeedback.Text = "O processo ainda nao foi iniciado pelo administrador.";
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    /// <summary>
    /// Alterna entre senha escondida e senha visivel, mantendo o mesmo texto.
    /// </summary>
    private void AlternarSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_senhaVisivel)
        {
            CaixaSenha.Password = CaixaTextoSenhaVisivel.Text;
            CaixaSenha.Visibility = Visibility.Visible;
            BordaSenhaVisivel.Visibility = Visibility.Collapsed;
            IconeOlhoAberto.Visibility = Visibility.Visible;
            IconeOlhoFechado.Visibility = Visibility.Collapsed;
        }
        else
        {
            CaixaTextoSenhaVisivel.Text = CaixaSenha.Password;
            CaixaSenha.Visibility = Visibility.Collapsed;
            BordaSenhaVisivel.Visibility = Visibility.Visible;
            IconeOlhoAberto.Visibility = Visibility.Collapsed;
            IconeOlhoFechado.Visibility = Visibility.Visible;
        }

        _senhaVisivel = !_senhaVisivel;
    }

    /// <summary>
    /// Ponto reservado para futura regra de recuperacao de senha.
    /// </summary>
    private void EsqueceuSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaRecuperacaoSenha());
    }
}
