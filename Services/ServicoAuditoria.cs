using System.IO;
using System.Text.Json;
using Alistar.App.Models;

namespace Alistar.App.Services;

/// <summary>
/// Persiste eventos importantes do sistema para consulta do administrador.
/// </summary>
public static class ServicoAuditoria
{
    private static readonly string CaminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string RaizDoProjeto = Path.GetFullPath(Path.Combine(CaminhoArquivo, @"..\..\..\"));
    private static readonly string CaminhoCompleto = Path.Combine(RaizDoProjeto, "auditoria.json");

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

    public static List<RegistroAuditoria> ObterTodos()
    {
        GarantirArquivoCriado();

        var conteudo = File.ReadAllText(CaminhoCompleto);
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<RegistroAuditoria>>(conteudo, OpcoesJson) ?? [];
    }

    public static void RegistrarAcao(string acao, string entidade, string descricao)
    {
        var usuario = ServicoAutenticacao.UsuarioAtual;

        AdicionarRegistro(new RegistroAuditoria
        {
            UsuarioNome = usuario?.Nome ?? "Sistema",
            UsuarioEmail = usuario?.Email ?? string.Empty,
            TipoUsuario = ServicoAutenticacao.UsuarioEhAdministrador(usuario) ? "Administrador" : "Entrevistador",
            Acao = acao,
            Entidade = entidade,
            Descricao = descricao
        });
    }

    public static void RegistrarAlteracao(string entidade, string campo, string valorAnterior, string valorNovo)
    {
        if (string.Equals(valorAnterior, valorNovo, StringComparison.Ordinal))
        {
            return;
        }

        var usuario = ServicoAutenticacao.UsuarioAtual;

        AdicionarRegistro(new RegistroAuditoria
        {
            UsuarioNome = usuario?.Nome ?? "Sistema",
            UsuarioEmail = usuario?.Email ?? string.Empty,
            TipoUsuario = ServicoAutenticacao.UsuarioEhAdministrador(usuario) ? "Administrador" : "Entrevistador",
            Acao = "Alteracao",
            Entidade = entidade,
            Campo = campo,
            ValorAnterior = valorAnterior,
            ValorNovo = valorNovo,
            Descricao = $"{campo}: '{valorAnterior}' para '{valorNovo}'"
        });
    }

    private static void AdicionarRegistro(RegistroAuditoria registro)
    {
        var registros = ObterTodos();
        registros.Add(registro);

        var json = JsonSerializer.Serialize(registros.OrderByDescending(item => item.DataHora).ToList(), OpcoesJson);
        File.WriteAllText(CaminhoCompleto, json);
    }

    private static void GarantirArquivoCriado()
    {
        Directory.CreateDirectory(RaizDoProjeto);

        if (!File.Exists(CaminhoCompleto))
        {
            File.WriteAllText(CaminhoCompleto, "[]");
        }
    }
}
