using System.Windows;
using System.Windows.Media;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Janela visual do primeiro passo de recuperacao de senha: e-mail e token.
/// </summary>
public partial class TelaRecuperacaoSenha : Window
{
    public TelaRecuperacaoSenha()
    {
        InitializeComponent();
    }

    private void EnviarTokenBotao_Click(object sender, RoutedEventArgs e)
    {
        var email = CaixaTextoEmailRecuperacao.Text.Trim();

        if (!EmailValido(email))
        {
            TextoFeedbackRecuperacao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C43D3D"));
            TextoFeedbackRecuperacao.Text = "Informe um e-mail valido com @ para continuar.";
            CaixaTextoEmailRecuperacao.Focus();
            return;
        }

        TextoFeedbackRecuperacao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D52"));
        TextoFeedbackRecuperacao.Text = "Token enviado para o e-mail informado.";
        PainelEmail.Visibility = Visibility.Collapsed;
        PainelToken.Visibility = Visibility.Visible;
        CaixaTextoToken.Focus();
    }

    private void ValidarTokenBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaNovaSenha());
    }

    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaLogin());
    }

    private static bool EmailValido(string valor)
    {
        var email = valor.Trim();
        var indiceArroba = email.IndexOf('@');
        return indiceArroba > 0 && indiceArroba == email.LastIndexOf('@') && indiceArroba < email.Length - 1;
    }
}
