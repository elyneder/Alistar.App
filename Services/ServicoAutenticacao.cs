using Alistar.App.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

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
    private static readonly string CaminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string RaizDoProjeto = Path.GetFullPath(Path.Combine(CaminhoArquivo, @"..\..\..\"));
    private static readonly string CaminhoCompleto = Path.Combine(RaizDoProjeto, "entrevistadores.json");

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

    public static readonly List<ContaUsuario> Contas = new List<ContaUsuario> {
        new ContaUsuario(){
            Nome = "Administrador",
            Email = "admin@alistar.com",
            Senha = "$2a$11$9c/gtvWSNiZ6pTQqjQfdJeg4JrJDY8nsrnqGeKnsp9wD1VrNppIhi",
            AdministradorGeral = true
        },
        new ContaUsuario(){
            Nome = "Administrador Geral 2",
            Email = "admin2@alistar.com",
            Senha = "$2a$11$9c/gtvWSNiZ6pTQqjQfdJeg4JrJDY8nsrnqGeKnsp9wD1VrNppIhi",
            AdministradorGeral = true
        },
        new ContaUsuario(){
            Nome = "Cabo 1",
            Email = "cabo1@gmail.com",
            Senha = "$2a$11$K/8V8RgPIc3xN7fzcHiwteGTtnZQHFwptbjAHZAhM4EjZL7Jzncl2",
            AdministradorGeral = false
        },
        new ContaUsuario(){
            Nome = "Cabo 2",
            Email = "cabo2@gmail.com",
            Senha = "$2a$11$wQYkWkOOZpKm0rOsI5mrdecfgHAqyBgwE4W3ahIXfIO1pJ4vRDUcm",
            AdministradorGeral = false
        }
    };

    /// <summary>
    /// Usuário logado atualmente. Fica nulo quando não há sessão ativa.
    /// </summary>
    public static ContaUsuario? UsuarioAtual { get; private set; }

    static ServicoAutenticacao()
    {
        CarregarLista();
        GarantirSegundoAdministrador();
    }

    /// <summary>
    /// Carrega as contas do arquivo JSON para a lista em memoria.
    /// </summary>
    public static void CarregarLista()
    {
        if (!File.Exists(CaminhoCompleto))
        {
            SalvarContas();
            return;
        }

        string json = File.ReadAllText(CaminhoCompleto);
        var data = JsonSerializer.Deserialize<List<ContaUsuario>>(json, OpcoesJson);

        if (data == null)
        {
            return;
        }

        Contas.Clear();
        Contas.AddRange(data);
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

        if (senhaValida)
        {
            ServicoAuditoria.RegistrarAcao("Login", "Acesso", $"Usuário {account.Email} entrou no sistema.");
        }

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
    /// Regra de permissao para os administradores gerais do sistema.
    /// </summary>
    public static bool UsuarioAtualEhAdministrador()
    {
        return UsuarioEhAdministrador(UsuarioAtual);
    }

    public static bool UsuarioEhAdministrador(ContaUsuario? conta)
    {
        return conta is not null &&
               (conta.AdministradorGeral);
    }
    public static bool UsuarioEhEntrevistador(ContaUsuario? conta)
    {
        if (conta.Entrevistador == true)
        {
            return conta is not null &&
                   (conta.Entrevistador);
        }
        else
        {
            return false;
        }
    }
    public static bool UsuarioEhMedico(ContaUsuario? conta)
    {
        if(conta.Medico == true)
        {
            return conta is not null &&
                   (conta.Medico);
        }
        else
        {
            return false;
        }
    }
    
    public static bool VerificarSeEhMedico()
    {
        return UsuarioEhMedico(UsuarioAtual);
    }

    public static string? obterCRM()
    {
        return UsuarioAtual.CRM;
    }

    public static bool VerificarSeEhEntrevistador()
    {
        return UsuarioEhEntrevistador(UsuarioAtual);
    }

    public static bool VerificacaoDeProcessoAberto (ConfiguracaoProcesso configuracao)
    {
        if (configuracao.ProcessoAberto == false) return false;

        return true;
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
            .Where(conta => !UsuarioEhAdministrador(conta))
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
    /// Remove o usuário atual da sessão.
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
    public static ResultadoCadastroUsuario Cadastrar(string nome, string email, string senha, bool medico, bool entrevistador, bool adm, string crm)
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return ResultadoCadastroUsuario.SemPermissao;
        }

        if (UsuarioExiste(email))
        {
            return ResultadoCadastroUsuario.EmailJaCadastrado;
        }

        try
        {
            if (medico)
            {
                Contas.Add(new ContaUsuario
                {
                    Nome = nome,
                    Email = email,
                    CRM = crm,
                    Senha = Seguranca.CriptografarSenha(senha),
                    AdministradorGeral = adm,
                    Medico = medico,
                    Entrevistador = entrevistador,
                });
            }else if (entrevistador)
            {
                Contas.Add(new ContaUsuario
                {
                    Nome = nome,
                    Email = email,
                    Senha = Seguranca.CriptografarSenha(senha),
                    AdministradorGeral = adm,
                    Medico = medico,
                    Entrevistador = entrevistador,
                });
            }else if (adm)
            {
                Contas.Add(new ContaUsuario
                {
                    Nome = nome,
                    Email = email,
                    Senha = Seguranca.CriptografarSenha(senha),
                    AdministradorGeral = adm,
                    Medico = medico,
                    Entrevistador = entrevistador,
                });
            }

            SalvarContas();
            ServicoAuditoria.RegistrarAcao("Cadastro", "Entrevistador", $"Administrador cadastrou o entrevistador {email}.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao salvar dados: " + ex.Message);
        }

        return ResultadoCadastroUsuario.Sucesso;
    }

    public static ContaUsuario? ObterEntrevistadorPorEmail(string email)
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return null;
        }

        return Contas.FirstOrDefault(conta =>
            !UsuarioEhAdministrador(conta) &&
            string.Equals(conta.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public static ResultadoCadastroUsuario AtualizarEntrevistador(string emailOriginal, string nome, string email, string? novaSenha)
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return ResultadoCadastroUsuario.SemPermissao;
        }

        var conta = ObterEntrevistadorPorEmail(emailOriginal);
        if (conta is null)
        {
            return ResultadoCadastroUsuario.NaoEncontrado;
        }

        var emailEmUso = Contas.Any(usuario =>
            !string.Equals(usuario.Email, emailOriginal, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase));

        if (emailEmUso)
        {
            return ResultadoCadastroUsuario.EmailJaCadastrado;
        }

        var emailAnterior = conta.Email;
        conta.Nome = nome.Trim();
        conta.Email = email.Trim();

        if (!string.IsNullOrWhiteSpace(novaSenha))
        {
            conta.Senha = Seguranca.CriptografarSenha(novaSenha.Trim());
        }

        SalvarContas();
        ServicoAuditoria.RegistrarAcao("Alteracao", "Entrevistador", $"Administrador atualizou o entrevistador {emailAnterior}.");
        return ResultadoCadastroUsuario.Sucesso;
    }

    public static bool ExcluirEntrevistador(string email)
    {
        if (!UsuarioAtualEhAdministrador())
        {
            return false;
        }

        var conta = ObterEntrevistadorPorEmail(email);
        if (conta is null)
        {
            return false;
        }

        Contas.Remove(conta);
        SalvarContas();
        ServicoAuditoria.RegistrarAcao("Exclusao", "Entrevistador", $"Administrador excluiu o entrevistador {email}.");
        return true;
    }

    private static void GarantirSegundoAdministrador()
    {
        var administradorOriginal = Contas.FirstOrDefault(conta =>
            string.Equals(conta.Email, "admin@alistar.com", StringComparison.OrdinalIgnoreCase));

        if (administradorOriginal is not null)
        {
            administradorOriginal.AdministradorGeral = true;
        }

        var segundoAdministrador = Contas.FirstOrDefault(conta =>
            string.Equals(conta.Email, "admin2@alistar.com", StringComparison.OrdinalIgnoreCase));

        if (segundoAdministrador is not null)
        {
            segundoAdministrador.AdministradorGeral = true;
            return;
        }

        if (administradorOriginal is null)
        {
            return;
        }

        Contas.Add(new ContaUsuario
        {
            Nome = "Administrador Geral 2",
            Email = "admin2@alistar.com",
            Senha = administradorOriginal.Senha,
            AdministradorGeral = true
        });

        SalvarContas();
    }

    private static void SalvarContas()
    {
        string jsonString = JsonSerializer.Serialize(Contas, OpcoesJson);
        File.WriteAllText(CaminhoCompleto, jsonString);
    }
}

/// <summary>
/// Resultado possível da tentativa de cadastro de um usuário.
/// </summary>
public enum ResultadoCadastroUsuario
{
    Sucesso,
    SemPermissao,
    EmailJaCadastrado,
    NaoEncontrado
}

public class EntrevistadorResumo
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
