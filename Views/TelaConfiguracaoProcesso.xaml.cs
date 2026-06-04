using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
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
    private List<EtapaPercentualFormulario> _etapasPercentuais = [];
    private List<ServidorSelecao> _servidoresSelecao = [];
    private int _passoAtual = PassoDadosGerais;
    private bool _dataFechamentoFoiAjustadaManualmente;
    private bool _atualizandoDataFechamentoAutomaticamente;

    public TelaConfiguracaoProcesso()
    {
        InitializeComponent();
        Activated += TelaConfiguracaoProcesso_Activated;
        CarregarTela();
        DefinirPasso(PassoDadosGerais);
        ServicoAuditoria.RegistrarAcao("Acesso", "Configuração do Processo", "Administrador geral abriu a configuração do processo.");
    }

    private void CarregarTela()
    {
        _configuracao = ServicoConfiguracaoProcesso.Obter();
        _configuracao.DataAbertura = DateTime.Today;
        _configuracao.DataFechamento = _configuracao.DataAbertura.AddMonths(3);
        _configuracao.DataNascimentoLimite = _configuracao.DataNascimentoLimite;

        DataAberturaPicker.SelectedDate = _configuracao.DataAbertura;
        DefinirDataFechamento(_configuracao.DataFechamento);
        CaixaAnoLimite.Text = ObterTextoInicialAnoLimite(_configuracao.AnoLimiteNascimento);
        CaixaTotalClassificados.Text = ObterTextoInicialTotalClassificados(_configuracao.TotalClassificados);
        _dataFechamentoFoiAjustadaManualmente = false;

        _etapasPercentuais = _configuracao.Etapas
            .Select(etapa => new EtapaPercentualFormulario
            {
                Numero = etapa.Numero,
                Nome = etapa.Nome
            })
            .ToList();

        ListaEtapasPercentuais.ItemsSource = _etapasPercentuais;
        AtualizarSaldosConscritos();
        AtualizarListaServidores();
    }

    private void TelaConfiguracaoProcesso_Activated(object? sender, EventArgs e)
    {
        AtualizarListaServidores(preservarSelecao: true);
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

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        AbrirTelaAuxiliar(new TelaCadastroUsuario(retornarParaTelaAnterior: true));
    }

    private void VerEntrevistadoresBotao_Click(object sender, RoutedEventArgs e)
    {
        AbrirTelaAuxiliar(new TelaEntrevistadores(retornarParaTelaAnterior: true));
    }

    private bool LerFormulario()
    {
        SalvarSelecaoServidores();

        if (!LerDadosGerais())
        {
            return false;
        }

        if (_passoAtual >= PassoEtapas && !LerPercentuaisEtapas())
        {
            return false;
        }

        AtualizarConfirmacao();
        return true;
    }

    private bool LerDadosGerais()
    {
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
            TextoFeedback.Text = "A data de fechamento não pode ser anterior à data de abertura.";
            return false;
        }

        _configuracao.DataAbertura = dataAbertura;
        _configuracao.DataFechamento = dataFechamento;
        _configuracao.AnoLimiteNascimento = anoLimite;
        _configuracao.TotalClassificados = totalClassificados;
        _configuracao.ProcessoAberto = true;

        return true;
    }

    private bool LerPercentuaisEtapas()
    {
        foreach (var etapaFormulario in _etapasPercentuais)
        {
            if (!ValidarInteiro(etapaFormulario.TextoPercentual, $"percentual da etapa {etapaFormulario.Numero}", out var percentual))
            {
                return false;
            }

            etapaFormulario.PercentualEliminacao = Math.Clamp(percentual, 0, 99);
        }

        foreach (var etapa in _configuracao.Etapas)
        {
            var etapaFormulario = _etapasPercentuais.FirstOrDefault(item => item.Numero == etapa.Numero);
            etapa.PercentualEliminacao = etapaFormulario?.PercentualEliminacao ?? 0;
        }

        return true;
    }

    private void AtualizarListaServidores(bool preservarSelecao = false)
    {
        var emailsSelecionados = preservarSelecao
            ? _servidoresSelecao
                .Where(item => item.Selecionado)
                .Select(item => item.Email)
                .Concat(_configuracao.ServidoresAutorizados)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _servidoresSelecao = ServicoAutenticacao.ObterEntrevistadores()
            .Select(servidor => new ServidorSelecao
            {
                Nome = servidor.Nome,
                Email = servidor.Email,
                Selecionado = emailsSelecionados.Contains(servidor.Email)
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

    private void AbrirTelaAuxiliar(Window janela)
    {
        SalvarSelecaoServidores();
        janela.Owner = this;
        Hide();
        janela.Show();
    }

    private void AtualizarConfirmacao()
    {
        var totaisConscritos = CalcularSaldosConscritos();

        TextoResumoConfirmacao.Text =
            $"Data de abertura: {_configuracao.DataAbertura:dd/MM/yyyy}\n" +
            $"Data de fechamento: {_configuracao.DataFechamento:dd/MM/yyyy}\n" +
            $"Ano limite: {_configuracao.AnoLimiteNascimento}\n" +
            $"Total esperado: {totaisConscritos.TotalEsperado}\n" +
            $"Total final: {totaisConscritos.TotalFinal}\n" +
            $"Total de classificados: {_configuracao.TotalClassificados}";

        TextoEtapasConfirmacao.Text = "Percentuais por etapa:\n" +
            string.Join("\n", _configuracao.Etapas.Select(etapa => $"{etapa.Numero}. {etapa.Nome}: {etapa.PercentualEliminacao}%"));

        var entrevistadores = _servidoresSelecao
            .Where(item => item.Selecionado)
            .Select(item => item.Rotulo)
            .ToList();

        TextoServidoresConfirmacao.Text = entrevistadores.Count == 0
            ? "Usuários autorizados: nenhum servidor selecionado."
            : "Usuários autorizados:\n" + string.Join("\n", entrevistadores);
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
            TextoFeedback.Text = "Informe o ano limite com 4 dígitos numéricos.";
            return false;
        }

        return true;
    }

    private string ObterTextoInicialAnoLimite(int anoLimite)
    {
        return anoLimite <= 0
            ? string.Empty
            : anoLimite.ToString();
    }

    private static string ObterTextoInicialTotalClassificados(int totalClassificados)
    {
        return totalClassificados <= 0
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

    private void TotalConscritosEsperados_TextChanged(object sender, TextChangedEventArgs e)
    {
        AtualizarSaldosConscritos();
    }

    private void PercentualEtapa_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox caixaPercentual &&
            caixaPercentual.DataContext is EtapaPercentualFormulario etapaFormulario)
        {
            etapaFormulario.TextoPercentual = caixaPercentual.Text;
        }

        AtualizarSaldosConscritos();
    }

    private void AtualizarSaldosConscritos()
    {
        var totaisConscritos = CalcularSaldosConscritos();

        ListaSaldosConscritos.ItemsSource = totaisConscritos.Saldos;
        TextoSaldoConscritosFinal.Text = $"Total Final: {totaisConscritos.TotalFinal}";
    }

    private (int TotalEsperado, int TotalFinal, List<SaldoConscritoEtapa> Saldos) CalcularSaldosConscritos()
    {
        var totalEsperado = ObterInteiroNaoNegativo(CaixaTotalConscritosEsperados.Text);
        var saldo = totalEsperado;
        var saldos = new List<SaldoConscritoEtapa>();

        foreach (var etapaFormulario in _etapasPercentuais)
        {
            var percentual = ObterPercentualCalculo(etapaFormulario.TextoPercentual);
            var quantidadeDispensada = (int)Math.Round(saldo * percentual / 100d, MidpointRounding.AwayFromZero);
            quantidadeDispensada = Math.Min(quantidadeDispensada, saldo);
            saldo -= quantidadeDispensada;

            saldos.Add(new SaldoConscritoEtapa
            {
                Numero = etapaFormulario.Numero,
                QuantidadeDispensada = quantidadeDispensada
            });
        }

        return (totalEsperado, saldo, saldos);
    }

    private static int ObterInteiroNaoNegativo(string texto)
    {
        return int.TryParse(texto.Trim(), out var valor) && valor > 0
            ? valor
            : 0;
    }

    private static int ObterPercentualCalculo(string texto)
    {
        return int.TryParse(texto.Trim(), out var percentual)
            ? Math.Clamp(percentual, 0, 99)
            : 0;
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
            PassoServidores => "Usuários Autorizados",
            _ => "Confirmação Final"
        };
    }

    private static string ObterDescricaoPasso(int passo)
    {
        return passo switch
        {
            PassoDadosGerais => "Informe datas e numeros principais do processo seletivo.",
            PassoEtapas => "Defina apenas a porcentagem de saída de cada etapa.",
            PassoServidores => "Selecione os usuários que poderão atuar no processo.",
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

public class EtapaPercentualFormulario
{
    public int Numero { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string TextoPercentual { get; set; } = string.Empty;

    public int PercentualEliminacao { get; set; }
}

public class SaldoConscritoEtapa
{
    public int Numero { get; set; }

    public int QuantidadeDispensada { get; set; }

    public string RotuloEtapa => $"{Numero} Etapa:";

    public string TextoReducao => QuantidadeDispensada == 0
        ? "0"
        : $"-{QuantidadeDispensada}";

    public Brush CorReducao => QuantidadeDispensada == 0
        ? Brushes.DarkOrange
        : Brushes.Firebrick;
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
