using System.Windows;
using Alistar.App.Services;

namespace Alistar.App;

public partial class RegisterWindow : Window
{
    private bool _isPasswordVisible = false;

    public RegisterWindow()
    {
        InitializeComponent();
    }

    private void TogglePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            RegisterPasswordTextBox.Password = RegisterPasswordVisible.Text;
            RegisterPasswordTextBox.Visibility = Visibility.Visible;
            RegisterPasswordVisibleBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            RegisterPasswordVisible.Text = RegisterPasswordTextBox.Password;
            RegisterPasswordTextBox.Visibility = Visibility.Collapsed;
            RegisterPasswordVisibleBorder.Visibility = Visibility.Visible;
        }

        _isPasswordVisible = !_isPasswordVisible;
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var email = RegisterEmailTextBox.Text.Trim();
        string password = _isPasswordVisible ? RegisterPasswordVisible.Text.Trim() : RegisterPasswordTextBox.Password.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            FeedbackTextBlock.Text = "Preencha nome, e-mail e senha.";
            return;
        }

        if (!AuthService.Register(name, email, password))
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

    private void BackToLogin_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}