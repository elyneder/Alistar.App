using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaCadastroUsuario : Window
{
    private bool _senhaVisivel;

    public TelaCadastroUsuario()
    {
        InitializeComponent();
    }

    private void AlternarSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_senhaVisivel)
        {
            CaixaSenhaUsuario.Password = CaixaTextoSenhaVisivelUsuario.Text;
            CaixaSenhaUsuario.Visibility = Visibility.Visible;
            BordaSenhaVisivelUsuario.Visibility = Visibility.Collapsed;
        }
        else
        {
            CaixaTextoSenhaVisivelUsuario.Text = CaixaSenhaUsuario.Password;
            CaixaSenhaUsuario.Visibility = Visibility.Collapsed;
            BordaSenhaVisivelUsuario.Visibility = Visibility.Visible;
        }

        _senhaVisivel = !_senhaVisivel;
    }

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
            TextoFeedbackCadastro.Text = "Apenas o usuario admin@alistar.com pode cadastrar um novo entrevistador.";
            return;
        }

        if (resultadoCadastro == ResultadoCadastroUsuario.EmailJaCadastrado)
        {
            TextoFeedbackCadastro.Text = "Ja existe uma conta com este e-mail.";
            return;
        }

        MessageBox.Show("Entrevistador cadastrado com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
