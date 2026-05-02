using System.IO;
using System.Text.Json;
using Alistar.App.Models;

namespace Alistar.App.Services;

public static class ServicoArmazenamentoConscritos
{
    private static List<Conscrito> conscritos = new List<Conscrito>();
    private static string caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    private static string raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
    private static string caminhoCompleto = Path.Combine(raizDoProjeto, "conscritos.json");

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

    static ServicoArmazenamentoConscritos()
    {
        CarregarLista();
    }

    public static void CarregarLista()
    {
        if (File.Exists(caminhoCompleto))
        {
            string json = File.ReadAllText(caminhoCompleto);
            var data = JsonSerializer.Deserialize<List<Conscrito>>(json);

            if (data != null)
            {
                conscritos.Clear();
                conscritos.AddRange(data);
            }
        }
    }

    public static List<Conscrito> ObterTodos()
    {
        var conteudo = File.ReadAllText(caminhoCompleto);

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

    public static void Excluir(string id)
    {
        var conscritos = ObterTodos();
        conscritos.RemoveAll(conscrito => conscrito.Id == id);
        SalvarTodos(conscritos);
    }

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

    private static void SalvarTodos(List<Conscrito> conscritos)
    {
        var json = JsonSerializer.Serialize(conscritos, OpcoesJson);
        File.WriteAllText(caminhoCompleto, json);
    }

}
