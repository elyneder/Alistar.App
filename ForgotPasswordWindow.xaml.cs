using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class ForgotPasswordWindow : Window
{
    public ForgotPasswordWindow()
    {
        InitializeComponent();
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        var email = RecoveryEmailTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            FeedbackTextBlock.Text = "Digite o e-mail para continuar.";
            return;
        }

        FeedbackTextBlock.Text = AuthService.UserExists(email)
            ? "E-mail encontrado. Aqui podemos conectar a redefinicao de senha na proxima etapa."
            : "Nenhuma conta foi localizada com esse e-mail.";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
