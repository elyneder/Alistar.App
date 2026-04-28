using System.Windows;

namespace Alistar.App;

public partial class TelaTerceiraEtapa : Window
{
    public TelaTerceiraEtapa()
    {
        InitializeComponent();
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
