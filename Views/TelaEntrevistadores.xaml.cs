using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Lista os entrevistadores cadastrados para consulta do administrador.
/// </summary>
public partial class TelaEntrevistadores : Window
{
    public TelaEntrevistadores()
    {
        InitializeComponent();
        CarregarEntrevistadores();
    }

    private void CarregarEntrevistadores()
    {
        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            MessageBox.Show("Apenas administradores podem ver os entrevistadores cadastrados.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        var entrevistadores = ServicoAutenticacao.ObterEntrevistadores();
        GradeEntrevistadores.ItemsSource = entrevistadores;
        TextoResumoEntrevistadores.Text = $"{entrevistadores.Count} entrevistador(es) cadastrado(s).";
        TextoTotalEntrevistadores.Text = entrevistadores.Count.ToString();
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaCadastroUsuario());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }
}
