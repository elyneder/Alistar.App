using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaLogin : Window
{
    private bool _senhaVisivel;

    public TelaLogin()
    {
        InitializeComponent();
    }

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

        var telaPainelControle = new TelaPainelControle();
        telaPainelControle.Show();
        Close();
    }

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

    private void EsqueceuSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        // sua lógica aqui
    }
}
