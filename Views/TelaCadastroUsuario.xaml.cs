using System.Windows;
using Alistar.App.Services;

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

    public TelaCadastroUsuario()
    {
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
        var senha = _senhaVisivel ? CaixaTextoSenhaVisivelUsuario.Text.Trim() : CaixaSenhaUsuario.Password.Trim();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            TextoFeedbackCadastro.Text = "Preencha nome, e-mail e senha.";
            return;
        }

        var resultadoCadastro = ServicoAutenticacao.Cadastrar(nome, email, senha);

        if (resultadoCadastro == ResultadoCadastroUsuario.SemPermissao)
        {
            TextoFeedbackCadastro.Text = "Apenas o usuário admin@alistar.com pode cadastrar um novo entrevistador.";
            return;
        }

        if (resultadoCadastro == ResultadoCadastroUsuario.EmailJaCadastrado)
        {
            TextoFeedbackCadastro.Text = "Já existe uma conta com este e-mail.";
            return;
        }

        MessageBox.Show("Entrevistador cadastrado com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    /// <summary>
    /// Fecha a janela sem salvar cadastro.
    /// </summary>
    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
