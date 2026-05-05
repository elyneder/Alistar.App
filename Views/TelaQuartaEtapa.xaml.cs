using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela reservada para a quarta etapa do fluxo de alistamento.
/// </summary>
/// <remarks>
/// Mantem o padrao das demais etapas para facilitar expansao futura.
/// </remarks>
public partial class TelaQuartaEtapa : Window
{
    public TelaQuartaEtapa()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fecha a janela da etapa.
    /// </summary>
    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }
}
