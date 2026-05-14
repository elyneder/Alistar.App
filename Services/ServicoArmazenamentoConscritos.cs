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
        //GarantirArmazenamentoCriado();

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
        // Na pratica, este metodo e o "SELECT *" do nosso projeto.
        // Ele le o JSON, transforma em objetos C# e devolve para as telas.
        //GarantirArmazenamentoCriado();

        //var conteudo = File.ReadAllText(caminhoCompleto);

        //if (string.IsNullOrWhiteSpace(conteudo))
        //{
        //    return [];
        //}

        //var conscritos = JsonSerializer.Deserialize<List<Conscrito>>(conteudo, OpcoesJson) ?? [];
        var houveNormalizacao = NormalizarConscritos(conscritos);

        //if (houveNormalizacao)
        //{
        //    SalvarTodos(conscritos);
        //}

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
    }


    /// <summary>
    /// Corrige registros antigos que possam estar sem Id ou sem situacao.
    /// </summary>
    private static bool NormalizarConscritos(List<Conscrito> conscritos)
    {
        // Como alguns cadastros antigos podem estar incompletos, este metodo
        // completa objetos nulos. Isso evita erro de NullReference ao abrir telas.
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

            if (conscrito.Entrevista_Vida_Pessoal is null)
            {
                conscrito.Entrevista_Vida_Pessoal = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Arrimo_De_Familia is null)
            {
                conscrito.Entrevista_Arrimo_De_Familia = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Cursos is null)
            {
                conscrito.Entrevista_Cursos = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Experiencia is null)
            {
                conscrito.Entrevista_Experiencia = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Habilitacao is null)
            {
                conscrito.Entrevista_Habilitacao = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Pre_Qualificacao_Imediata is null)
            {
                conscrito.Entrevista_Pre_Qualificacao_Imediata = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Esportes is null)
            {
                conscrito.Entrevista_Esportes = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Lazer is null)
            {
                conscrito.Entrevista_Lazer = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Saude is null)
            {
                conscrito.Entrevista_Saude = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Infracao is null)
            {
                conscrito.Entrevista_Infracao = new();
                houveMudanca = true;
            }

            if (conscrito.Entrevista_Medica is null)
            {
                conscrito.Entrevista_Medica = new();
                houveMudanca = true;
            }
        }

        return houveMudanca;
    }

    /// <summary>
    /// Serializa a lista completa e grava no arquivo JSON.
    /// </summary>
    //private static void SalvarTodos(List<Conscrito> conscritos)
    //{
    //    var json = JsonSerializer.Serialize(conscritos, OpcoesJson);
    //    File.WriteAllText(caminhoCompleto, json);
    //}

    /// <summary>
    /// Cria a pasta/arquivo de armazenamento antes de qualquer leitura.
    /// </summary>
    //private static void GarantirArmazenamentoCriado()
    //{
    //    Directory.CreateDirectory(raizDoProjeto);

    //    if (!File.Exists(caminhoCompleto))
    //    {
    //        File.WriteAllText(caminhoCompleto, "[]");
    //    }
    //}
}
