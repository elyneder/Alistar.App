using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class MainWindow : Window
{
    private bool _isPasswordVisible = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailTextBox.Text.Trim();

        string password;

        if (_isPasswordVisible)
            password = PasswordVisible.Text.Trim();
        else
            password = PasswordBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            FeedbackTextBlock.Text = "Preencha o e-mail e a senha para continuar.";
            return;
        }

        if (!AuthService.ValidateLogin(email, password))
        {
            FeedbackTextBlock.Text = "E-mail ou senha inválidos.";
            return;
        }

        var dashboardWindow = new DashboardWindow();
        dashboardWindow.Show();
        Close();
    }

    private void TogglePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            PasswordBox.Password = PasswordVisible.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordVisibleBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordVisible.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordVisibleBorder.Visibility = Visibility.Visible;
        }

        _isPasswordVisible = !_isPasswordVisible;
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