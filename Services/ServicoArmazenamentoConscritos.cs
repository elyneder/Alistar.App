using System.IO;
using System.Text.Json;
using Alistar.App.Models;

namespace Alistar.App.Services;

/// <summary>
/// Servico responsavel por persistir os conscritos em arquivo JSON local.
/// </summary>
/// <remarks>
/// Como o projeto nao usa banco de dados, esta classe funciona como uma camada
/// simples de repositorio: le, adiciona, atualiza e exclui registros.
/// </remarks>
public static class ServicoArmazenamentoConscritos
{
    // Pasta do usuario no Windows. Evita depender da pasta do projeto para dados reais.
    private static readonly string DiretorioArmazenamento =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Alistar");

    // Arquivo atual e arquivo antigo usado para migracao simples de nome.
    private static readonly string CaminhoArmazenamento = Path.Combine(DiretorioArmazenamento, "conscritos.json");
    private static readonly string CaminhoArmazenamentoAntigo = Path.Combine(DiretorioArmazenamento, "conscripts.json");

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Carrega todos os conscritos salvos no JSON.
    /// </summary>
    public static List<Conscrito> ObterTodos()
    {
        GarantirArmazenamentoCriado();

        var conteudo = File.ReadAllText(CaminhoArmazenamento);

        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return [];
        }

        var conscritos = JsonSerializer.Deserialize<List<Conscrito>>(conteudo, OpcoesJson) ?? [];
        var houveNormalizacao = NormalizarConscritos(conscritos);

        if (houveNormalizacao)
        {
            SalvarTodos(conscritos);
        }

        return conscritos;
    }

    /// <summary>
    /// Adiciona um novo conscrito e garante valores padrao obrigatorios.
    /// </summary>
    public static void Adicionar(Conscrito conscrito)
    {
        var conscritos = ObterTodos();
        if (string.IsNullOrWhiteSpace(conscrito.Id))
        {
            conscrito.Id = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrWhiteSpace(conscrito.Situacao))
        {
            conscrito.Situacao = "Indefinido";
        }

        conscritos.Add(conscrito);
        SalvarTodos(conscritos);
    }

    /// <summary>
    /// Substitui um conscrito existente usando o Id como chave.
    /// </summary>
    public static void Atualizar(Conscrito conscritoAtualizado)
    {
        var conscritos = ObterTodos();
        var indice = conscritos.FindIndex(conscrito => conscrito.Id == conscritoAtualizado.Id);

        if (indice < 0)
        {
            return;
        }

        conscritos[indice] = conscritoAtualizado;
        SalvarTodos(conscritos);
    }

    /// <summary>
    /// Remove um conscrito pelo Id.
    /// </summary>
    public static void Excluir(string id)
    {
        var conscritos = ObterTodos();
        conscritos.RemoveAll(conscrito => conscrito.Id == id);
        SalvarTodos(conscritos);
    }

    /// <summary>
    /// Corrige registros antigos que possam estar sem Id ou sem situacao.
    /// </summary>
    private static bool NormalizarConscritos(List<Conscrito> conscritos)
    {
        var houveMudanca = false;

        foreach (var conscrito in conscritos)
        {
            if (string.IsNullOrWhiteSpace(conscrito.Id))
            {
                conscrito.Id = Guid.NewGuid().ToString();
                houveMudanca = true;
            }

            if (string.IsNullOrWhiteSpace(conscrito.Situacao))
            {
                conscrito.Situacao = "Indefinido";
                houveMudanca = true;
            }
        }

        return houveMudanca;
    }

    /// <summary>
    /// Serializa a lista completa e grava no arquivo JSON.
    /// </summary>
    private static void SalvarTodos(List<Conscrito> conscritos)
    {
        Directory.CreateDirectory(DiretorioArmazenamento);
        var json = JsonSerializer.Serialize(conscritos, OpcoesJson);
        File.WriteAllText(CaminhoArmazenamento, json);
    }

    /// <summary>
    /// Cria a pasta/arquivo de armazenamento antes de qualquer leitura.
    /// </summary>
    private static void GarantirArmazenamentoCriado()
    {
        Directory.CreateDirectory(DiretorioArmazenamento);

        if (!File.Exists(CaminhoArmazenamento) && File.Exists(CaminhoArmazenamentoAntigo))
        {
            File.Copy(CaminhoArmazenamentoAntigo, CaminhoArmazenamento);
        }

        if (!File.Exists(CaminhoArmazenamento))
        {
            File.WriteAllText(CaminhoArmazenamento, "[]");
        }
    }
}
