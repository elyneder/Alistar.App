using System.IO;
using System.Text.Json;
using System.Windows;
using Alistar.App.Models;

namespace Alistar.App.Services;

/// <summary>
/// Armazena as regras gerais do processo seletivo em JSON local.
/// </summary>
public static class ServicoConfiguracaoProcesso
{
    //private static readonly string CaminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
    //private static readonly string RaizDoProjeto = Path.GetFullPath(Path.Combine(CaminhoArquivo, @"..\..\..\"));
    //private static readonly string CaminhoCompleto = Path.Combine(RaizDoProjeto, "processo-config.json");

    public static List <ConfiguracaoProcesso> _configuracoes = new List<ConfiguracaoProcesso>();

    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true
    };

    public static ConfiguracaoProcesso Obter()
    {
        //GarantirArquivoCriado();

        //var conteudo = File.ReadAllText(CaminhoCompleto);
        var configuracao = _configuracoes.FirstOrDefault() ?? new ConfiguracaoProcesso();

        NormalizarDatas(configuracao);
        NormalizarEtapas(configuracao);
        CalcularEliminados(configuracao);
        return configuracao;
    }

    public static void Salvar(ConfiguracaoProcesso configuracao)
    {
        NormalizarDatas(configuracao);
        NormalizarEtapas(configuracao);
        CalcularEliminados(configuracao);

        var anterior = Obter();
        RegistrarAlteracoes(anterior, configuracao);
        _configuracoes.Add(configuracao);

        //var json = JsonSerializer.Serialize(configuracao, OpcoesJson);
        //File.WriteAllText(CaminhoCompleto, json);
    }

    public static void AdicionarEntrevistadorAoProcesso(string email)
    {
        var configuracao = Obter();
        var servidoresAutorizados = configuracao.ServidoresAutorizados;

        bool existeEsseEmailNoProcesso = servidoresAutorizados.Contains(email);

        if (existeEsseEmailNoProcesso) MessageBox.Show("Esse usuário já está no processo");

        if (!existeEsseEmailNoProcesso)
        {
            configuracao.ServidoresAutorizados.Add(email);
            Salvar(configuracao);
            MessageBox.Show("Usuário adicionado no processo");
        }
    }

    public static bool VerSeEntervistadorPartipaDoProcesso(string email)
    {
        var configuracao = Obter();
        var servidoresAutorizados = configuracao.ServidoresAutorizados;

        bool existeEsseEmailNoProcesso = servidoresAutorizados.Contains(email);

        if (existeEsseEmailNoProcesso) return true;

        if (!existeEsseEmailNoProcesso) return false;

        return false;
    }

    public static bool UsuarioPodeAcessarEtapa(int numeroEtapa, string? emailUsuario)
    {
        if (ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(emailUsuario))
        {
            return false;
        }

        return Obter().ServidoresAutorizados.Contains(emailUsuario, StringComparer.OrdinalIgnoreCase);
    }

    public static bool ProcessoFoiConfigurado()
    {
        var configuracao = Obter();

        return configuracao.AnoLimiteNascimento > 0 &&
               configuracao.TotalClassificados > 0;
    }

    private static void RegistrarAlteracoes(ConfiguracaoProcesso anterior, ConfiguracaoProcesso atual)
    {
        ServicoAuditoria.RegistrarAlteracao("Configuração do Processo", "Data de fechamento", anterior.DataFechamento.ToShortDateString(), atual.DataFechamento.ToShortDateString());
        ServicoAuditoria.RegistrarAlteracao("Configuração do Processo", "Data de abertura", anterior.DataAbertura.ToShortDateString(), atual.DataAbertura.ToShortDateString());
        ServicoAuditoria.RegistrarAlteracao("Configuração do Processo", "Ano limite", anterior.AnoLimiteNascimento.ToString(), atual.AnoLimiteNascimento.ToString());
        ServicoAuditoria.RegistrarAlteracao("Configuração do Processo", "Total de classificados", anterior.TotalClassificados.ToString(), atual.TotalClassificados.ToString());
        ServicoAuditoria.RegistrarAlteracao(
            "Configuração do Processo",
            "Servidores autorizados",
            string.Join(", ", anterior.ServidoresAutorizados.OrderBy(email => email)),
            string.Join(", ", atual.ServidoresAutorizados.OrderBy(email => email)));

        foreach (var etapaAtual in atual.Etapas)
        {
            var etapaAnterior = anterior.Etapas.FirstOrDefault(etapa => etapa.Numero == etapaAtual.Numero);
            if (etapaAnterior is null)
            {
                continue;
            }

            var entidade = $"Etapa {etapaAtual.Numero} - {etapaAtual.Nome}";
            ServicoAuditoria.RegistrarAlteracao(entidade, "Percentual de eliminação", etapaAnterior.PercentualEliminacao.ToString(), etapaAtual.PercentualEliminacao.ToString());
            ServicoAuditoria.RegistrarAlteracao(entidade, "Quantidade eliminada", etapaAnterior.QuantidadeEliminados.ToString(), etapaAtual.QuantidadeEliminados.ToString());
        }
    }

    private static void CalcularEliminados(ConfiguracaoProcesso configuracao)
    {
        foreach (var etapa in configuracao.Etapas)
        {
            etapa.PercentualEliminacao = Math.Clamp(etapa.PercentualEliminacao, 0, 99);
            etapa.QuantidadeEliminados = (int)Math.Ceiling(configuracao.TotalClassificados * (etapa.PercentualEliminacao / 100m));
        }
    }

    private static void NormalizarDatas(ConfiguracaoProcesso configuracao)
    {
        if (configuracao.DataAbertura == default)
        {
            configuracao.DataAbertura = DateTime.Today;
        }

        configuracao.DataAbertura = configuracao.DataAbertura.Date;

        if (configuracao.DataFechamento == default)
        {
            configuracao.DataFechamento = configuracao.DataAbertura.AddMonths(3);
        }

        configuracao.DataFechamento = configuracao.DataFechamento.Date;
    }

    private static void NormalizarEtapas(ConfiguracaoProcesso configuracao)
    {
        if (configuracao.Etapas.Count > 0)
        {
            if (configuracao.ServidoresAutorizados.Count == 0)
            {
                configuracao.ServidoresAutorizados = configuracao.Etapas
                    .SelectMany(etapa => etapa.EntrevistadoresAutorizados)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return;
        }

        configuracao.Etapas = new ConfiguracaoProcesso().Etapas;
    }

    //private static void GarantirArquivoCriado()
    //{
    //    Directory.CreateDirectory(RaizDoProjeto);

    //    if (!File.Exists(CaminhoCompleto))
    //    {
    //        var json = JsonSerializer.Serialize(new ConfiguracaoProcesso(), OpcoesJson);
    //        File.WriteAllText(CaminhoCompleto, json);
    //    }
    //}
}
