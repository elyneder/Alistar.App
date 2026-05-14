using System.Windows;

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
        TextoFeedbackRecuperacao.Text = "Token enviado para o e-mail informado.";
        PainelEmail.Visibility = Visibility.Collapsed;
        PainelToken.Visibility = Visibility.Visible;
        BotaoValidarToken.Visibility = Visibility.Visible;
        CaixaTextoToken.Focus();
    }

    private void ValidarTokenBotao_Click(object sender, RoutedEventArgs e)
    {
        var telaNovaSenha = new TelaNovaSenha
        {
            Owner = Owner
        };

        Close();
        telaNovaSenha.ShowDialog();
    }

    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
