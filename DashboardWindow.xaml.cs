using System.Windows;
using System.Windows.Controls;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();
        LoadConscripts();
        ShowHomeView();
    }

    private void SaveConscriptButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var motherName = MotherNameTextBox.Text.Trim();
        var fatherName = FatherNameTextBox.Text.Trim();
        var birthDate = BirthDateTextBox.Text.Trim();
        var address = AddressTextBox.Text.Trim();
        var city = CityTextBox.Text.Trim();
        var education = (EducationComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(motherName) ||
            string.IsNullOrWhiteSpace(fatherName) ||
            string.IsNullOrWhiteSpace(birthDate) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(education))
        {
            RegisterFeedbackTextBlock.Text = "Preencha todos os campos da ficha do candidato.";
            return;
        }

        var conscript = new Conscript
        {
            Name = name,
            MotherName = motherName,
            FatherName = fatherName,
            BirthDate = birthDate,
            Address = address,
            City = city,
            Education = education
        };

        ConscriptStorageService.Add(conscript);
        RegisterFeedbackTextBlock.Text = "Ficha cadastrada com sucesso.";
        LoadConscripts();
        ClearFormFields();
        ShowListView();
    }

    private void ShowRegisterViewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowRegisterView();
    }

    private void ShowHomeViewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowHomeView();
    }

    private void ShowListViewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowListView();
    }

    private void ClearFormButton_Click(object sender, RoutedEventArgs e)
    {
        ClearFormFields();
        RegisterFeedbackTextBlock.Text = string.Empty;
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new MainWindow();
        loginWindow.Show();
        Close();
    }

    private void LoadConscripts()
    {
        var conscripts = ConscriptStorageService.GetAll();
        ConscriptsDataGrid.ItemsSource = conscripts;
        ConscriptsCountTextBlock.Text = conscripts.Count.ToString();
        HomeConscriptsCountTextBlock.Text = conscripts.Count.ToString();
    }

    private void ShowHomeView()
    {
        HomeViewBorder.Visibility = Visibility.Visible;
        RegisterViewBorder.Visibility = Visibility.Collapsed;
        ListViewBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowRegisterView()
    {
        HomeViewBorder.Visibility = Visibility.Collapsed;
        RegisterViewBorder.Visibility = Visibility.Visible;
        ListViewBorder.Visibility = Visibility.Collapsed;
    }

    private void ShowListView()
    {
        HomeViewBorder.Visibility = Visibility.Collapsed;
        RegisterViewBorder.Visibility = Visibility.Collapsed;
        ListViewBorder.Visibility = Visibility.Visible;
    }

    private void ClearFormFields()
    {
        NameTextBox.Text = string.Empty;
        MotherNameTextBox.Text = string.Empty;
        FatherNameTextBox.Text = string.Empty;
        BirthDateTextBox.Text = string.Empty;
        AddressTextBox.Text = string.Empty;
        CityTextBox.Text = string.Empty;
        EducationComboBox.SelectedIndex = 0;
    }
}
