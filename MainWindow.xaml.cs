using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailTextBox.Text.Trim();
        var password = PasswordTextBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            FeedbackTextBlock.Text = "Preencha o e-mail e a senha para continuar.";
            return;
        }

        if (!AuthService.ValidateLogin(email, password))
        {
            FeedbackTextBlock.Text = "E-mail ou senha invalidos.";
            return;
        }

        var dashboardWindow = new DashboardWindow();
        dashboardWindow.Show();
        Close();
    }

    private void ForgotPasswordHyperlink_Click(object sender, RoutedEventArgs e)
    {
        var forgotPasswordWindow = new ForgotPasswordWindow
        {
            Owner = this
        };
        forgotPasswordWindow.ShowDialog();
    }

    private void CreateAccountHyperlink_Click(object sender, RoutedEventArgs e)
    {
        var registerWindow = new RegisterWindow
        {
            Owner = this
        };
        registerWindow.ShowDialog();
    }
}
