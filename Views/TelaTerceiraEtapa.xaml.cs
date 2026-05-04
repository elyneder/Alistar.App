using System.Windows;

namespace Alistar.App;

/// <summary>
/// Tela reservada para a terceira etapa do fluxo de alistamento.
/// </summary>
/// <remarks>
/// Atualmente funciona como estrutura inicial, pronta para receber campos e regras futuras.
/// </remarks>
public partial class TelaTerceiraEtapa : Window
{
    public TelaTerceiraEtapa()
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
