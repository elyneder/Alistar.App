using System.Windows;

namespace Alistar.App.Services;

/// <summary>
/// Centraliza a troca de telas para manter apenas uma janela principal aberta.
/// </summary>
public static class ServicoNavegacao
{
    public static void Trocar(Window janelaAtual, Window proximaJanela)
    {
        Application.Current.MainWindow = proximaJanela;
        proximaJanela.Show();
        janelaAtual.Close();
    }
}
