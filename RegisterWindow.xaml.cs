using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var email = RegisterEmailTextBox.Text.Trim();
        var password = RegisterPasswordTextBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            FeedbackTextBlock.Text = "Preencha nome, e-mail e senha.";
            return;
        }

        if (!ServicoDeAutenticacao.Register(name, email, password))
        {
            FeedbackTextBlock.Text = "Ja existe uma conta com este e-mail.";
            return;
        }

        MessageBox.Show("Conta cadastrada com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
