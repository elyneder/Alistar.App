using System.Windows;

namespace Alistar.App;

/// <summary>
/// Janela visual para cadastro da nova senha apos a validacao do token.
/// </summary>
public partial class TelaNovaSenha : Window
{
    public TelaNovaSenha()
    {
        InitializeComponent();
    }

    private void SalvarSenhaBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedbackNovaSenha.Text = "Senha alterada com sucesso. Voce ja pode entrar novamente.";
    }

    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
