using System.Windows;
using Alistar.App.Services;

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
        TextoFeedbackNovaSenha.Text = "Senha alterada com sucesso. Você já pode entrar novamente.";
        ServicoNavegacao.Trocar(this, new TelaLogin());
    }

    private void CancelarBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaLogin());
    }
}
