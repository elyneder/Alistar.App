using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
    private bool _dataFechamentoFoiAjustadaManualmente;
    private bool _atualizandoDataFechamentoAutomaticamente;

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
        _configuracao.DataAbertura = DateTime.Today;
        _configuracao.DataFechamento = _configuracao.DataAbertura.AddMonths(3);

        DataAberturaPicker.SelectedDate = _configuracao.DataAbertura;
        DefinirDataFechamento(_configuracao.DataFechamento);
        CaixaAnoLimite.Text = ObterTextoInicialAnoLimite(_configuracao.AnoLimiteNascimento);
        CaixaTotalClassificados.Text = ObterTextoInicialTotalClassificados(_configuracao.TotalClassificados);
        _dataFechamentoFoiAjustadaManualmente = false;

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
        var dataAbertura = (DataAberturaPicker.SelectedDate ?? DateTime.Today).Date;
        var dataFechamento = (DataFechamentoPicker.SelectedDate ?? dataAbertura.AddMonths(3)).Date;

        if (!ValidarAnoLimite(CaixaAnoLimite.Text, out var anoLimite) ||
            !ValidarInteiro(CaixaTotalClassificados.Text, "total de classificados", out var totalClassificados))
        {
            return false;
        }

        if (totalClassificados <= 0)
        {
            TextoFeedback.Text = "O total de classificados precisa ser maior que zero.";
            return false;
        }

        if (dataFechamento < dataAbertura)
        {
            TextoFeedback.Text = "A data de fechamento nao pode ser anterior a data de abertura.";
            return false;
        }

        _configuracao.DataAbertura = dataAbertura;
        _configuracao.DataFechamento = dataFechamento;
        _configuracao.AnoLimiteNascimento = anoLimite;
        _configuracao.TotalClassificados = totalClassificados;

        foreach (var etapa in _configuracao.Etapas)
        {
            etapa.PercentualEliminacao = Math.Clamp(etapa.PercentualEliminacao, 0, 99);
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
            $"Data de fechamento: {_configuracao.DataFechamento:dd/MM/yyyy}\n" +
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

    private bool ValidarAnoLimite(string texto, out int valor)
    {
        valor = 0;
        var textoLimpo = texto.Trim();

        if (textoLimpo.Length != 4 || !int.TryParse(textoLimpo, out valor))
        {
            TextoFeedback.Text = "Informe o ano limite com 4 digitos numericos.";
            return false;
        }

        return true;
    }

    private static string ObterTextoInicialAnoLimite(int anoLimite)
    {
        var anoPadraoAntigo = DateTime.Today.Year - 18;
        return anoLimite <= 0 || anoLimite == anoPadraoAntigo
            ? string.Empty
            : anoLimite.ToString();
    }

    private static string ObterTextoInicialTotalClassificados(int totalClassificados)
    {
        return totalClassificados <= 0 || totalClassificados == 50
            ? string.Empty
            : totalClassificados.ToString();
    }

    private void CampoNumerico_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void CampoNumerico_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var texto = e.DataObject.GetData(typeof(string)) as string ?? string.Empty;
        if (!texto.All(char.IsDigit))
        {
            e.CancelCommand();
        }
    }

    private void DataAberturaPicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_atualizandoDataFechamentoAutomaticamente || _dataFechamentoFoiAjustadaManualmente)
        {
            return;
        }

        var dataAbertura = (DataAberturaPicker.SelectedDate ?? DateTime.Today).Date;
        DefinirDataFechamento(dataAbertura.AddMonths(3));
    }

    private void DataFechamentoPicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_atualizandoDataFechamentoAutomaticamente)
        {
            return;
        }

        var dataAbertura = (DataAberturaPicker.SelectedDate ?? DateTime.Today).Date;
        var dataFechamento = (DataFechamentoPicker.SelectedDate ?? dataAbertura.AddMonths(3)).Date;
        _dataFechamentoFoiAjustadaManualmente = dataFechamento != dataAbertura.AddMonths(3);
    }

    private void DefinirDataFechamento(DateTime dataFechamento)
    {
        _atualizandoDataFechamentoAutomaticamente = true;
        DataFechamentoPicker.SelectedDate = dataFechamento.Date;
        _atualizandoDataFechamentoAutomaticamente = false;
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
            PassoDadosGerais => "Informe datas e numeros principais do processo seletivo.",
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
