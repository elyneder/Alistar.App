using Alistar.App.Models;

namespace Alistar.App.Services;

public static class ServicoAutenticacao
{
    private static readonly List<ContaUsuario> Contas =
    [
        new ContaUsuario
        {
            Nome = "Administrador",
            Email = "admin@alistar.com",
            Senha = "123456"
        }
    ];

    public static ContaUsuario? UsuarioAtual { get; private set; }

    public static bool ValidarLogin(string email, string senha)
    {
        var contaEncontrada = Contas.FirstOrDefault(conta =>
            string.Equals(conta.Email, email, StringComparison.OrdinalIgnoreCase) &&
            conta.Senha == senha);

        UsuarioAtual = contaEncontrada;
        return contaEncontrada is not null;
    }

    public static bool UsuarioExiste(string email)
    {
        return Contas.Any(conta =>
            string.Equals(conta.Email, email, StringComparison.OrdinalIgnoreCase));
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
            Senha = senha
        });

        return ResultadoCadastroUsuario.Sucesso;
    }
}

public enum ResultadoCadastroUsuario
{
    Sucesso,
    SemPermissao,
    EmailJaCadastrado
}
