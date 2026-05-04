using System.Windows;

namespace Alistar.App;

/// <summary>
/// Tela reservada para a quinta etapa do fluxo de alistamento.
/// </summary>
/// <remarks>
/// Ainda nao possui regras de negocio, mas ja segue a organizacao visual do sistema.
/// </remarks>
public partial class TelaQuintaEtapa : Window
{
    public TelaQuintaEtapa()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fecha a janela da etapa.
    /// </summary>
    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
