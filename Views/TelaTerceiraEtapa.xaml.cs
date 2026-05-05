using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela de fallback para a terceira etapa do fluxo de alistamento.
/// </summary>
/// <remarks>
/// O acesso principal da Entrevista Tecnica reaproveita o wizard da primeira etapa
/// para revisar e atualizar a ficha completa do conscrito.
/// </remarks>
public partial class TelaTerceiraEtapa : Window
{
    public TelaTerceiraEtapa()
    {
        InitializeComponent();

        ServicoNavegacao.Trocar(this, new TelaPrimeiraEtapa(null, true, true));
    }

    /// <summary>
    /// Fecha a janela da etapa.
    /// </summary>
    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }
}
