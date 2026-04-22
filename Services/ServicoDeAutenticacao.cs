using Alistar.App.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Alistar.App.Services;

public static class ServicoDeAutenticacao
{
    private static List<Administradores> listaAdm = new List<Administradores>();
    private static string caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static string raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
    private static string caminhoCompleto = Path.Combine(raizDoProjeto, "entrevistadores.json");

    static ServicoDeAutenticacao()
    {
        CarregarLista();
    }

    public static void CarregarLista()
    {
        if (File.Exists(caminhoCompleto))
        {
            string json = File.ReadAllText(caminhoCompleto);
            var data = JsonSerializer.Deserialize<List<Administradores>>(json);
        
            if(data != null)
            {
                listaAdm.Clear();
                listaAdm.AddRange(data);
            }
        }
    }

    public static bool ValidacaoDeLogin(string email, string password)
    {
        var account = listaAdm.FirstOrDefault(acc => string.Equals(acc.Email, email, StringComparison.OrdinalIgnoreCase));

        if(account == null)
        {
            return false;
        }

        return Seguranca.VerificarSenha(password, account.Password);

    }

    public static bool VerificacaoDeUsuario(string email)
    {
        return listaAdm.Any(account =>
            string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public static bool RegistroDeUsuario(string name, string email, string password)
    {
        if (VerificacaoDeUsuario(email))
        {
            return false;
        }

        listaAdm.Add(new Administradores
        {
            Name = name,
            Email = email,
            Password = Seguranca.CriptografarSenha(password)
        });

        try
        {
            string jsonString = JsonSerializer.Serialize(listaAdm, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(caminhoCompleto, jsonString);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao salvar dados: " + ex.Message);
        }
  
        return true;
    }
}
