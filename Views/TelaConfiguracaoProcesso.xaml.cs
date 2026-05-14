using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela exibida para administradores antes do painel principal.
/// </summary>
public partial class TelaConfiguracaoProcesso : Window
{
    private const int PassoDadosGerais = 0;
    private const int PassoEtapas = 1;
    private const int PassoServidores = 2;
    private const int PassoConfirmacao = 3;

    private static readonly Brush FundoEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F5EA")!);
    private static readonly Brush FundoEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2FBF6")!);
    private static readonly Brush FundoEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")!);
    private static readonly Brush BordaEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#196743")!);
    private static readonly Brush BordaEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8CC7A3")!);
    private static readonly Brush BordaEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9E1DD")!);

    private ConfiguracaoProcesso _configuracao = new();
    private List<ServidorSelecao> _servidoresSelecao = [];
    private int _passoAtual = PassoDadosGerais;

    public TelaConfiguracaoProcesso()
    {
        InitializeComponent();
        CarregarTela();
        DefinirPasso(PassoDadosGerais);
        ServicoAuditoria.RegistrarAcao("Acesso", "Configuração do Processo", "Administrador geral abriu a configuração do processo.");
    }

    private void CarregarTela()
    {
        _configuracao = ServicoConfiguracaoProcesso.Obter();

        DataAberturaPicker.SelectedDate = _configuracao.DataAbertura;
        CaixaAnoLimite.Text = _configuracao.AnoLimiteNascimento.ToString();
        CaixaTotalClassificados.Text = _configuracao.TotalClassificados.ToString();

        ListaEtapasPercentuais.ItemsSource = _configuracao.Etapas;
        AtualizarListaServidores();
    }

    private void AnteriorBotao_Click(object sender, RoutedEventArgs e)
    {
        DefinirPasso(Math.Max(PassoDadosGerais, _passoAtual - 1));
    }

    private void ProximoBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedback.Text = string.Empty;

        if (!LerFormulario())
        {
            return;
        }

        if (_passoAtual < PassoConfirmacao)
        {
            DefinirPasso(_passoAtual + 1);
            return;
        }

        ServicoConfiguracaoProcesso.Salvar(_configuracao);
        ServicoAuditoria.RegistrarAcao("Configuração", "Processo", "Administrador geral concluiu a configuração do processo.");
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }

    private bool LerFormulario()
    {
        SalvarSelecaoServidores();

        if (!ValidarInteiro(CaixaAnoLimite.Text, "ano limite", out var anoLimite) ||
            !ValidarInteiro(CaixaTotalClassificados.Text, "total de classificados", out var totalClassificados))
        {
            return false;
        }

        if (totalClassificados <= 0)
        {
            TextoFeedback.Text = "O total de classificados precisa ser maior que zero.";
            return false;
        }

        _configuracao.DataAbertura = DataAberturaPicker.SelectedDate ?? DateTime.Today;
        _configuracao.AnoLimiteNascimento = anoLimite;
        _configuracao.TotalClassificados = totalClassificados;

        foreach (var etapa in _configuracao.Etapas)
        {
            etapa.PercentualEliminacao = Math.Clamp(etapa.PercentualEliminacao, 0, 100);
        }

        AtualizarConfirmacao();
        return true;
    }

    private void AtualizarListaServidores()
    {
        var autorizados = _configuracao.ServidoresAutorizados.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _servidoresSelecao = ServicoAutenticacao.ObterEntrevistadores()
            .Select(servidor => new ServidorSelecao
            {
                Nome = servidor.Nome,
                Email = servidor.Email,
                Selecionado = autorizados.Contains(servidor.Email)
            })
            .ToList();

        ListaServidores.ItemsSource = _servidoresSelecao;
    }

    private void SalvarSelecaoServidores()
    {
        _configuracao.ServidoresAutorizados = _servidoresSelecao
            .Where(item => item.Selecionado)
            .Select(item => item.Email)
            .ToList();

        foreach (var etapa in _configuracao.Etapas)
        {
            etapa.EntrevistadoresAutorizados = _configuracao.ServidoresAutorizados.ToList();
        }
    }

    private void AtualizarConfirmacao()
    {
        TextoResumoConfirmacao.Text =
            $"Data de abertura: {_configuracao.DataAbertura:dd/MM/yyyy}\n" +
            $"Ano limite: {_configuracao.AnoLimiteNascimento}\n" +
            $"Total de classificados: {_configuracao.TotalClassificados}";

        TextoEtapasConfirmacao.Text = "Percentuais por etapa:\n" +
            string.Join("\n", _configuracao.Etapas.Select(etapa => $"{etapa.Numero}. {etapa.Nome}: {etapa.PercentualEliminacao}%"));

        var entrevistadores = _servidoresSelecao
            .Where(item => item.Selecionado)
            .Select(item => item.Rotulo)
            .ToList();

        TextoServidoresConfirmacao.Text = entrevistadores.Count == 0
            ? "Entrevistadores autorizados: nenhum servidor selecionado."
            : "Entrevistadores autorizados:\n" + string.Join("\n", entrevistadores);
    }

    private bool ValidarInteiro(string texto, string campo, out int valor)
    {
        if (int.TryParse(texto.Trim(), out valor))
        {
            return true;
        }

        TextoFeedback.Text = $"Informe um valor numérico válido para {campo}.";
        return false;
    }

    private void DefinirPasso(int passo)
    {
        _passoAtual = passo;

        PainelDadosGerais.Visibility = passo == PassoDadosGerais ? Visibility.Visible : Visibility.Collapsed;
        PainelEtapas.Visibility = passo == PassoEtapas ? Visibility.Visible : Visibility.Collapsed;
        PainelServidores.Visibility = passo == PassoServidores ? Visibility.Visible : Visibility.Collapsed;
        PainelConfirmacao.Visibility = passo == PassoConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        BotaoAnterior.Visibility = passo > PassoDadosGerais ? Visibility.Visible : Visibility.Collapsed;
        BotaoAnterior.IsEnabled = passo > PassoDadosGerais;
        BotaoProximo.Content = passo == PassoConfirmacao ? "Concluir" : "Próximo";

        TextoTituloFormulario.Text = ObterTituloPasso(passo);
        TextoDescricaoFormulario.Text = ObterDescricaoPasso(passo);
        TextoDescricaoCabecalho.Text = ObterDescricaoPasso(passo);

        AtualizarCartaoWizard(CartaoWizardDados, passo, PassoDadosGerais);
        AtualizarCartaoWizard(CartaoWizardEtapas, passo, PassoEtapas);
        AtualizarCartaoWizard(CartaoWizardServidores, passo, PassoServidores);
        AtualizarCartaoWizard(CartaoWizardConfirmacao, passo, PassoConfirmacao);

        if (passo == PassoConfirmacao)
        {
            LerFormulario();
        }

        ScrollFormularioConfiguracao.ScrollToHome();
    }

    private static string ObterTituloPasso(int passo)
    {
        return passo switch
        {
            PassoDadosGerais => "Dados Gerais",
            PassoEtapas => "Etapas do Processo",
            PassoServidores => "Entrevistadores Autorizados",
            _ => "Confirmação Final"
        };
    }

    private static string ObterDescricaoPasso(int passo)
    {
        return passo switch
        {
            PassoDadosGerais => "Informe os dados principais do processo seletivo.",
            PassoEtapas => "Defina apenas a porcentagem de saída de cada etapa.",
            PassoServidores => "Selecione os entrevistadores que poderão atuar no processo.",
            _ => "Revise os dados antes de concluir a configuração."
        };
    }

    private static void AtualizarCartaoWizard(System.Windows.Controls.Border cartao, int passoAtual, int passoCartao)
    {
        if (passoCartao == passoAtual)
        {
            cartao.Background = FundoEtapaAtiva;
            cartao.BorderBrush = BordaEtapaAtiva;
            return;
        }

        if (passoCartao < passoAtual)
        {
            cartao.Background = FundoEtapaConcluida;
            cartao.BorderBrush = BordaEtapaConcluida;
            return;
        }

        cartao.Background = FundoEtapaInativa;
        cartao.BorderBrush = BordaEtapaInativa;
    }
}

public class ServidorSelecao : INotifyPropertyChanged
{
    private bool _selecionado;

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Rotulo => $"{Nome} - {Email}";

    public bool Selecionado
    {
        get => _selecionado;
        set
        {
            if (_selecionado == value)
            {
                return;
            }

            _selecionado = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
