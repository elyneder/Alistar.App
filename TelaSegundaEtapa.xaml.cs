using System.Windows;

namespace Alistar.App;

public partial class TelaSegundaEtapa : Window
{
    public TelaSegundaEtapa()
    {
        InitializeComponent();
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
