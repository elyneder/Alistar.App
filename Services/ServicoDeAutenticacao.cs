using Alistar.App.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Alistar.App.Services;

public static class AuthService
{
    private static List<UserAccount> listUsers = new List<UserAccount>();
    private static string caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static string raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
    private static string caminhoCompleto = Path.Combine(raizDoProjeto, "entrevistadores.json");

    static AuthService()
    {
        LoadAccounts();
    }

    public static void LoadAccounts()
    {
        if (File.Exists(caminhoCompleto))
        {
            string json = File.ReadAllText(caminhoCompleto);
            var data = JsonSerializer.Deserialize<List<UserAccount>>(json);
        
            if(data != null)
            {
                listUsers.Clear();
                listUsers.AddRange(data);
            }
        }
    }

    public static bool ValidateLogin(string email, string password)
    {
        var account = listUsers.FirstOrDefault(acc => string.Equals(acc.Email, email, StringComparison.OrdinalIgnoreCase));

        if(account == null)
        {
            return false;
        }

        return Security.VerifyPassword(password, account.Password);

    }

    public static bool UserExists(string email)
    {
        return listUsers.Any(account =>
            string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public static bool Register(string name, string email, string password)
    {
        if (UserExists(email))
        {
            return false;
        }

        listUsers.Add(new UserAccount
        {
            Name = name,
            Email = email,
            Password = Security.HashPassword(password)
        });

        try
        {
            string jsonString = JsonSerializer.Serialize(listUsers, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(caminhoCompleto, jsonString);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao salvar dados: " + ex.Message);
        }
  
        return true;
    }
}
