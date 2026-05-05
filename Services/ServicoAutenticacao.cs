using Alistar.App.Models;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Xml.Linq;

namespace Alistar.App.Services;

/// <summary>
/// Controla login, usuario atual e cadastro de entrevistadores.
/// </summary>
/// <remarks>
/// Esta classe mantem a lista de contas em memoria e sincroniza com
/// entrevistadores.json. Ela tambem define a regra de administrador do sistema.
/// </remarks>
public static class ServicoAutenticacao
{
    // Lista em memoria usada durante a execucao do aplicativo.
    private static List<ContaUsuario> Contas = new List<ContaUsuario>();

    // Caminho calculado ate o arquivo entrevistadores.json do projeto.
    private static string caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static string raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
    private static string caminhoCompleto = Path.Combine(raizDoProjeto, "entrevistadores.json");

    /// <summary>
    /// Usuario logado atualmente. Fica nulo quando nao ha sessao ativa.
    /// </summary>
    public static ContaUsuario? UsuarioAtual { get; private set; }

    static ServicoAutenticacao()
    {
        CarregarLista();
    }

    /// <summary>
    /// Carrega as contas do arquivo JSON para a lista em memoria.
    /// </summary>
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

    /// <summary>
    /// Valida email e senha digitados na tela de login.
    /// </summary>
    public static bool ValidarLogin(string email, string senha)
    {
        var account = Contas.FirstOrDefault(acc => string.Equals(acc.Email, email, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            return false;
        }

        bool senhaValida = Seguranca.VerificarSenha(senha, account.Senha);

        UsuarioAtual = senhaValida ? account : null;

        return senhaValida;
    }

    /// <summary>
    /// Verifica se ja existe uma conta cadastrada com o e-mail informado.
    /// </summary>
    public static bool UsuarioExiste(string email)
    {
        return Contas.Any(account =>
              string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Regra simples de permissao: somente o email admin@alistar.com e administrador.
    /// </summary>
    public static bool UsuarioAtualEhAdministrador()
    {
        return string.Equals(
            UsuarioAtual?.Email,
            "admin@alistar.com",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retorna os entrevistadores cadastrados sem expor senha ou hash.
    /// </summary>
    public static List<EntrevistadorResumo> ObterEntrevistadores()
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return [];
        }

        return Contas
            .Where(conta => !string.Equals(conta.Email, "admin@alistar.com", StringComparison.OrdinalIgnoreCase))
            .OrderBy(conta => conta.Nome)
            .ThenBy(conta => conta.Email)
            .Select(conta => new EntrevistadorResumo
            {
                Nome = conta.Nome,
                Email = conta.Email
            })
            .ToList();
    }

    /// <summary>
    /// Remove o usuario atual da sessao.
    /// </summary>
    public static void EncerrarSessao()
    {
        UsuarioAtual = null;
    }

    public static void ConfirmarSaidaSistema(Window janelaAtual)
    {
        var resultado = MessageBox.Show(
            "Tem certeza que deseja sair do sistema?",
            "Confirmar saída",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado != MessageBoxResult.Yes)
        {
            return;
        }

        EncerrarSessao();
        var telaLogin = new Alistar.App.TelaLogin();
        telaLogin.Show();
        janelaAtual.Close();
    }

    /// <summary>
    /// Cadastra novo entrevistador, desde que o usuario atual seja administrador.
    /// </summary>
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

/// <summary>
/// Resultado possivel da tentativa de cadastro de um usuario.
/// </summary>
public enum ResultadoCadastroUsuario
{
    Sucesso,
    SemPermissao,
    EmailJaCadastrado
}

public class EntrevistadorResumo
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
