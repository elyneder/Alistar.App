using System.IO;
using System.Text.Json;
using Alistar.App.Models;

namespace Alistar.App.Services;

public static class ServicoArmazenamentoConscritos
{
    private static readonly string DiretorioArmazenamento =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Alistar");

    private static readonly string CaminhoArmazenamento = Path.Combine(DiretorioArmazenamento, "conscritos.json");
    private static readonly string CaminhoArmazenamentoAntigo = Path.Combine(DiretorioArmazenamento, "conscripts.json");

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

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
        Directory.CreateDirectory(DiretorioArmazenamento);
        var json = JsonSerializer.Serialize(conscritos, OpcoesJson);
        File.WriteAllText(CaminhoArmazenamento, json);
    }

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
