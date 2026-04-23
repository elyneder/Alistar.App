using Alistar.App.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Xml.Linq;

namespace Alistar.App.Services;

public static class ServicoAutenticacao
{
    private static List<ContaUsuario> Contas = new List<ContaUsuario>();
    private static string caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static string raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
    private static string caminhoCompleto = Path.Combine(raizDoProjeto, "entrevistadores.json");

    public static ContaUsuario? UsuarioAtual { get; private set; }

    static ServicoAutenticacao()
    {
        CarregarLista();
    }

    public static void CarregarLista()
    {
        if (File.Exists(caminhoCompleto))
        {
            string json = File.ReadAllText(caminhoCompleto);
            var data = JsonSerializer.Deserialize<List<ContaUsuario>>(json);

            if (data != null)
            {
                Contas.Clear();
                Contas.AddRange(data);
            }
        }
    }

    public static bool ValidarLogin(string email, string senha)
    {
        var account = Contas.FirstOrDefault(acc => string.Equals(acc.Email, email, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            return false;
        }

        bool senhaValida = Seguranca.VerificarSenha(senha, account.Senha);

        UsuarioAtual = account;

        return senhaValida;
    }

    public static bool UsuarioExiste(string email)
    {
        return Contas.Any(account =>
              string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public static bool UsuarioAtualEhAdministrador()
    {
        return string.Equals(
            UsuarioAtual?.Email,
            "admin@alistar.com",
            StringComparison.OrdinalIgnoreCase);
    }

    public static void EncerrarSessao()
    {
        UsuarioAtual = null;
    }

    public static ResultadoCadastroUsuario Cadastrar(string nome, string email, string senha)
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return ResultadoCadastroUsuario.SemPermissao;
        }

        if (UsuarioExiste(email))
        {
            return ResultadoCadastroUsuario.EmailJaCadastrado;
        }

        Contas.Add(new ContaUsuario
        {
            Nome = nome,
            Email = email,
            Senha = Seguranca.CriptografarSenha(senha)
        });

        try
        {
            string jsonString = JsonSerializer.Serialize(Contas, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(caminhoCompleto, jsonString);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao salvar dados: " + ex.Message);
        }

        return ResultadoCadastroUsuario.Sucesso;
    }
}

public enum ResultadoCadastroUsuario
{
    Sucesso,
    SemPermissao,
    EmailJaCadastrado
}
