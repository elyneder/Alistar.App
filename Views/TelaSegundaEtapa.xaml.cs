using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela da segunda etapa, responsavel pela avaliacao medica do conscrito.
/// </summary>
/// <remarks>
/// A identificacao usa busca por RA para reaproveitar o cadastro feito na primeira
/// etapa. Depois disso, os blocos medicos sao preenchidos em formato de wizard.
/// </remarks>
public partial class TelaSegundaEtapa : Window
{
    // Constantes que representam cada passo do wizard da avaliacao medica.
    private const int EtapaWizardIdentificacao = 0;
    private const int EtapaWizardAvaliacaoFisica = 1;
    private const int EtapaWizardVisao = 2;
    private const int EtapaWizardTesteAuditivo = 3;
    private const int EtapaWizardExameGeral = 4;
    private const int EtapaWizardHistoricoMedico = 5;
    private const int EtapaWizardSaudeMental = 6;
    private const int EtapaWizardConfirmacao = 7;
    private const int TotalEtapasWizard = 8;

    // Cores dos cards de progresso do wizard.
    private static readonly Brush FundoEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F5EA")!);
    private static readonly Brush FundoEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2FBF6")!);
    private static readonly Brush FundoEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")!);
    private static readonly Brush BordaEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#196743")!);
    private static readonly Brush BordaEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8CC7A3")!);
    private static readonly Brush BordaEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9E1DD")!);

    // Estado local usado pelo wizard, pela mascara de CPF e pela busca de conscritos.
    private int _etapaWizardAtual = EtapaWizardIdentificacao;
    private bool _atualizandoMascara;
    private bool _atualizandoBuscaConscrito;
    private bool _dadosAnaliseConfirmados;
    private int _quantidadePessoasAnalise = 1;
    private int _quantidadePessoasProcessadas;
    private readonly bool _modoReavaliacaoMedica;
    private List<Conscrito> _conscritosCarregados = [];

    public TelaSegundaEtapa(bool modoReavaliacaoMedica = false)
    {
        _modoReavaliacaoMedica = modoReavaliacaoMedica;

        InitializeComponent();
        ComboTipoAnaliseMedica.SelectedIndex = 0;
        AplicarModoTela();
        RegistrarEventosCamposCondicionais();
        AtualizarCamposCondicionais();
        CarregarResumo();
        DefinirEtapaWizard(EtapaWizardIdentificacao);

        VerEntrevistadores.Visibility = Visibility.Collapsed;
        VerEntrevistadores.IsEnabled = false;
        CadastrarEntrevistador.Visibility = Visibility.Collapsed;
        CadastrarEntrevistador.IsEnabled = false;
        BotaoListaConscritos.Visibility = Visibility.Collapsed;
        BotaoListaConscritos.IsEnabled = false;
    }

    private void AplicarModoTela()
    {
        if (!_modoReavaliacaoMedica)
        {
            return;
        }

        Title = "Alistar | Quarta Etapa";
        TextoTituloPagina.Text = "Quarta Etapa";
        TextoDescricaoPagina.Text = "Reavaliação médica do conscrito.";
    }

    /// <summary>
    /// Liga perguntas Sim/Nao da avaliacao medica aos respectivos campos de observacao.
    /// </summary>
    private void RegistrarEventosCamposCondicionais()
    {
        ComboTipoAnaliseMedica.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboResultadoAptidao.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboProblemaPostura.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboTesteAuditivo.SelectionChanged += CampoCondicional_SelectionChanged;
    }

    private void CampoCondicional_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarCamposCondicionais();
    }

    /// <summary>
    /// Observacoes medicas so ficam obrigatorias quando a alteracao correspondente e marcada como Sim.
    /// </summary>
    private void AtualizarCamposCondicionais()
    {
        var tipoAnalise = ObterTextoSelecionado(ComboTipoAnaliseMedica);
        DefinirVisibilidadeCondicional(
            string.Equals(tipoAnalise, "Grupal", StringComparison.OrdinalIgnoreCase),
            PainelQuantidadePessoas);

        var resultado = ObterTextoSelecionado(ComboResultadoAptidao);
        var aptoComRestricao = string.Equals(resultado, "Apto com restrição", StringComparison.OrdinalIgnoreCase);
        var inapto = string.Equals(resultado, "Inapto", StringComparison.OrdinalIgnoreCase);
        DefinirVisibilidadeCondicional(aptoComRestricao, PainelRestricaoAptidao);
        DefinirVisibilidadeCondicional(aptoComRestricao || inapto, PainelProblemaMedico, PainelCidMedico);
        TextoRotuloProblemaMedico.Text = inapto ? "Motivo:" : "Qual problema:";

        DefinirVisibilidadeCondicional(
            RespostaEhSim(ObterTextoSelecionado(ComboProblemaPostura)),
            ObterElementoNomeado("PainelObservacaoPostura"));
        DefinirVisibilidadeCondicional(
            RespostaEhSim(ObterTextoSelecionado(ComboTesteAuditivo)),
            ObterElementoNomeado("PainelObservacaoAuditiva"));
    }

    private UIElement? ObterElementoNomeado(string nome)
    {
        return FindName(nome) as UIElement;
    }

    private static void DefinirVisibilidadeCondicional(bool mostrar, params UIElement?[] elementos)
    {
        foreach (var elemento in elementos)
        {
            if (elemento is null)
            {
                continue;
            }

            elemento.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;

            if (!mostrar)
            {
                LimparControles(elemento);
            }
        }
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPrimeiraEtapa(abrirListaAoIniciar: true));
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaCadastroUsuario());
    }

    private void VerEntrevistadoresBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaEntrevistadores());
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.ConfirmarSaidaSistema(this);
    }

    /// <summary>
    /// Botao principal: avanca etapas e, na confirmacao, grava a avaliacao medica.
    /// </summary>
    private void SalvarBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedback.Foreground = Brushes.Firebrick;

        if (_etapaWizardAtual == EtapaWizardIdentificacao && !_dadosAnaliseConfirmados)
        {
            if (!ValidarDadosAnalise())
            {
                return;
            }

            _dadosAnaliseConfirmados = true;
            _quantidadePessoasProcessadas = 0;
            TextoFeedback.Text = string.Empty;
            AtualizarWizardFormulario();
            ScrollFormularioSegundaEtapa.ScrollToHome();
            return;
        }

        var nome = CaixaTextoNome.Text.Trim();
        var cpf = CaixaTextoCPF.Text.Trim();
        var ra = CaixaTextoRA.Text.Trim();

        if (_etapaWizardAtual == EtapaWizardIdentificacao)
        {
            TentarSelecionarConscritoPeloRaDigitado();
            nome = CaixaTextoNome.Text.Trim();
            cpf = CaixaTextoCPF.Text.Trim();
            ra = CaixaTextoRA.Text.Trim();

            if (!ValidarEtapaAtualFormulario(nome, cpf, ra))
            {
                return;
            }

            DefinirEtapaWizard(EtapaWizardConfirmacao);
            return;
        }

        if (!ValidarFormularioCompleto(nome, cpf, ra))
        {
            return;
        }

        SalvarAvaliacaoConscritoAtual(nome, cpf, ra);
    }

    private void SalvarAvaliacaoConscritoAtual(string nome, string cpf, string ra)
    {
        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();
        var conscrito = conscritos.FirstOrDefault(item =>
            string.Equals(item.CPF, cpf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.RA, ra, StringComparison.OrdinalIgnoreCase));

        var novoCadastro = conscrito is null;
        conscrito ??= new Conscrito();

        if (_modoReavaliacaoMedica && novoCadastro)
        {
            TextoFeedback.Text = "Localize um conscrito cadastrado pelo RA antes de salvar a reavaliação.";
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        if (!_modoReavaliacaoMedica && !novoCadastro && EntrevistaMedicaPossuiDados(conscrito.Entrevista_Medica))
        {
            TextoFeedback.Text = "Este conscrito já possui avaliação médica. Use a Quarta Etapa para visualizar ou editar a ficha.";
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        conscrito.Nome = nome;
        conscrito.CPF = cpf;
        conscrito.RA = ra;
        conscrito.Faltoso = false;

        if (_modoReavaliacaoMedica)
        {
            conscrito.QuartaEtapaConcluida = true;
        }
        else
        {
            conscrito.SegundaEtapaConcluida = true;
        }

        PreencherDadosMedicos(conscrito);

        try
        {
            if (novoCadastro)
            {
                ServicoArmazenamentoConscritos.Adicionar(conscrito);
            }
            else
            {
                ServicoArmazenamentoConscritos.Atualizar(conscrito);
            }
        }
        catch (InvalidOperationException ex)
        {
            TextoFeedback.Text = ex.Message;
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        _quantidadePessoasProcessadas++;
        CarregarResumo();

        if (_quantidadePessoasProcessadas < _quantidadePessoasAnalise)
        {
            PrepararProximoConscrito();
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void MarcarFaltaBotao_Click(object sender, RoutedEventArgs e)
    {
        var conscrito = ObterConscritoSelecionadoOuPesquisado();
        if (conscrito is null)
        {
            TextoFeedback.Foreground = Brushes.Firebrick;
            TextoFeedback.Text = "Selecione ou pesquise um conscrito pelo RA antes de marcar falta.";
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        conscrito.Faltoso = true;
        ServicoArmazenamentoConscritos.Atualizar(conscrito);
        _quantidadePessoasProcessadas++;
        CarregarResumo();

        if (_dadosAnaliseConfirmados && _quantidadePessoasProcessadas < _quantidadePessoasAnalise)
        {
            PrepararProximoConscrito();
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    /// <summary>
    /// Garante que o conscrito foi selecionado/preenchido antes de avancar.
    /// </summary>
    private bool ValidarDadosAnalise()
    {
        var tipoAnalise = ObterTextoSelecionado(ComboTipoAnaliseMedica);
        var quantidadeTexto = CaixaTextoQuantidadePessoas.Text.Trim();

        if (string.IsNullOrWhiteSpace(tipoAnalise))
        {
            TextoFeedback.Text = "Selecione o tipo de análise antes de avançar.";
            ComboTipoAnaliseMedica.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(CaixaTextoCRM.Text))
        {
            TextoFeedback.Text = "Informe o CRM do médico antes de avançar.";
            CaixaTextoCRM.Focus();
            return false;
        }

        if (!CrmEstaValido(CaixaTextoCRM.Text))
        {
            TextoFeedback.Text = "Informe o CRM no formato 000000-AA.";
            CaixaTextoCRM.Focus();
            return false;
        }

        if (string.Equals(tipoAnalise, "Individual", StringComparison.OrdinalIgnoreCase))
        {
            CaixaTextoQuantidadePessoas.Text = "1";
            _quantidadePessoasAnalise = 1;
            return true;
        }

        if (int.TryParse(quantidadeTexto, out var quantidade) && quantidade > 0)
        {
            _quantidadePessoasAnalise = quantidade;
            return true;
        }

        TextoFeedback.Text = "Informe a quantidade de pessoas da análise grupal.";
        CaixaTextoQuantidadePessoas.Focus();
        return false;
    }

    private bool ValidarIdentificacao(string nome, string cpf, string ra)
    {
        if (!string.IsNullOrWhiteSpace(nome) &&
            !string.IsNullOrWhiteSpace(cpf) &&
            !string.IsNullOrWhiteSpace(ra))
        {
            return true;
        }

        TextoFeedback.Text = "Preencha nome, CPF e RA para avançar.";
        DefinirEtapaWizard(EtapaWizardIdentificacao);
        return false;
    }

    private bool ValidarCidQuandoNecessario()
    {
        if (PainelCidMedico.Visibility != Visibility.Visible)
        {
            return true;
        }

        if (CidEstaValido(CaixaTextoCID.Text))
        {
            return true;
        }

        TextoFeedback.Text = "Informe o CID no formato A00 ou A00.AAA.";
        CaixaTextoCID.Focus();
        return false;
    }

    /// <summary>
    /// Valida somente os campos da etapa atual do wizard.
    /// </summary>
    private bool ValidarEtapaAtualFormulario(string nome, string cpf, string ra)
    {
        if (_etapaWizardAtual == EtapaWizardIdentificacao && !ValidarIdentificacao(nome, cpf, ra))
        {
            return false;
        }

        if (_etapaWizardAtual == EtapaWizardIdentificacao && !ValidarCidQuandoNecessario())
        {
            return false;
        }

        var areaValidacao = ObterAreaValidacaoEtapaAtual();
        if (FormularioEstaPreenchido(areaValidacao))
        {
            return true;
        }

        TextoFeedback.Text = "Preencha todos os campos desta etapa antes de avançar.";
        return false;
    }

    /// <summary>
    /// Valida todas as secoes antes de permitir salvar.
    /// </summary>
    private bool ValidarFormularioCompleto(string nome, string cpf, string ra)
    {
        if (!ValidarIdentificacao(nome, cpf, ra))
        {
            return false;
        }

        if (FormularioEstaPreenchido(PainelFormularioSegundaEtapa))
        {
            return true;
        }

        TextoFeedback.Text = "Preencha todos os campos do formulário antes de salvar.";
        return false;
    }

    /// <summary>
    /// Retorna a secao que deve ser validada de acordo com a etapa atual.
    /// </summary>
    private DependencyObject ObterAreaValidacaoEtapaAtual()
    {
        return _etapaWizardAtual switch
        {
            EtapaWizardIdentificacao => SecaoIdentificacao,
            EtapaWizardAvaliacaoFisica => SecaoAvaliacaoFisica,
            EtapaWizardVisao => SecaoVisao,
            EtapaWizardTesteAuditivo => SecaoTesteAuditivo,
            EtapaWizardExameGeral => SecaoExameGeral,
            EtapaWizardHistoricoMedico => SecaoHistoricoMedico,
            EtapaWizardSaudeMental => SecaoSaudeMental,
            _ => PainelFormularioSegundaEtapa
        };
    }

    /// <summary>
    /// Percorre controles visiveis e verifica se TextBox/ComboBox estao preenchidos.
    /// </summary>
    private bool FormularioEstaPreenchido(DependencyObject elemento)
    {
        if (elemento is UIElement { Visibility: Visibility.Collapsed })
        {
            return true;
        }

        if (elemento == CaixaPesquisaRA ||
            elemento == ListaResultadosPesquisaRA ||
            elemento == TextoResultadoPesquisaRA)
        {
            return true;
        }

        if (elemento is TextBox caixaTexto && string.IsNullOrWhiteSpace(caixaTexto.Text))
        {
            caixaTexto.Focus();
            return false;
        }

        if (elemento is ComboBox comboBox && string.IsNullOrWhiteSpace(ObterTextoSelecionado(comboBox)))
        {
            comboBox.Focus();
            return false;
        }

        var quantidadeFilhos = VisualTreeHelper.GetChildrenCount(elemento);
        for (var indice = 0; indice < quantidadeFilhos; indice++)
        {
            if (!FormularioEstaPreenchido(VisualTreeHelper.GetChild(elemento, indice)))
            {
                return false;
            }
        }

        return true;
    }

    private void VoltarEtapaWizardBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_etapaWizardAtual == EtapaWizardIdentificacao)
        {
            if (_dadosAnaliseConfirmados)
            {
                _dadosAnaliseConfirmados = false;
                AtualizarWizardFormulario();
            }

            return;
        }

        DefinirEtapaWizard(_etapaWizardAtual - 1);
    }

    private void DefinirEtapaWizard(int etapa)
    {
        _etapaWizardAtual = Math.Max(EtapaWizardIdentificacao, Math.Min(EtapaWizardConfirmacao, etapa));
        if (_etapaWizardAtual == EtapaWizardIdentificacao)
        {
            _dadosAnaliseConfirmados = false;
        }

        AtualizarWizardFormulario();
        ScrollFormularioSegundaEtapa.ScrollToHome();
    }

    /// <summary>
    /// Atualiza secoes visiveis e textos de apoio conforme a etapa atual.
    /// </summary>
    private void AtualizarWizardFormulario()
    {
        var mostrarFormularioCompleto = _etapaWizardAtual == EtapaWizardConfirmacao;
        var tituloFormulario = _modoReavaliacaoMedica ? "Reavaliação Médica" : "Etapa Médica";

        var mostrarDadosAnalise = _etapaWizardAtual == EtapaWizardIdentificacao && !_dadosAnaliseConfirmados;
        var mostrarBuscaConscrito = mostrarFormularioCompleto ||
            _etapaWizardAtual != EtapaWizardIdentificacao ||
            _dadosAnaliseConfirmados;

        SecaoIdentificacao.Visibility = Visibility.Visible;
        PainelDadosAnalise.Visibility = mostrarDadosAnalise || mostrarFormularioCompleto ? Visibility.Visible : Visibility.Collapsed;
        PainelBuscaConscrito.Visibility = mostrarBuscaConscrito ? Visibility.Visible : Visibility.Collapsed;
        PainelDadosConscrito.Visibility = mostrarBuscaConscrito ? Visibility.Visible : Visibility.Collapsed;
        PainelAptidaoInicial.Visibility = mostrarBuscaConscrito ? Visibility.Visible : Visibility.Collapsed;
        SecaoAvaliacaoFisica.Visibility = Visibility.Collapsed;
        SecaoVisao.Visibility = Visibility.Collapsed;
        SecaoTesteAuditivo.Visibility = Visibility.Collapsed;
        SecaoExameGeral.Visibility = Visibility.Collapsed;
        SecaoHistoricoMedico.Visibility = Visibility.Collapsed;
        SecaoSaudeMental.Visibility = Visibility.Collapsed;

        AtualizarIndicadorEtapa(IndicadorEtapaIdentificacao, EtapaWizardIdentificacao);
        AtualizarIndicadorEtapaPorIntervalo(IndicadorEtapaExames, EtapaWizardAvaliacaoFisica, EtapaWizardSaudeMental);
        AtualizarIndicadorEtapa(IndicadorEtapaConfirmacao, EtapaWizardConfirmacao);

        BotaoVoltarEtapaWizard.Visibility = _etapaWizardAtual > EtapaWizardIdentificacao || _dadosAnaliseConfirmados
            ? Visibility.Visible
            : Visibility.Collapsed;
        IconeAvancarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Collapsed : Visibility.Visible;
        IconeSalvarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        switch (_etapaWizardAtual)
        {
            case EtapaWizardIdentificacao:
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = _modoReavaliacaoMedica
                    ? "Busque o RA do conscrito para abrir a ficha médica completa."
                    : "Comece pela identificação do conscrito. Depois avance pelos exames em blocos separados.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case >= EtapaWizardAvaliacaoFisica and <= EtapaWizardHistoricoMedico:
                var nomeBloco = ObterNomeBlocoWizard(_etapaWizardAtual);
                ObterSecaoBlocoWizard(_etapaWizardAtual).Visibility = Visibility.Visible;
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = "Preencha este bloco médico e avance pela seta para continuar.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case EtapaWizardSaudeMental:
                SecaoSaudeMental.Visibility = Visibility.Visible;
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = "Preencha o último bloco médico antes da confirmação final.";
                TextoBotaoSalvar.Text = "Ir para confirmação";
                break;

            default:
                SecaoAvaliacaoFisica.Visibility = Visibility.Collapsed;
                SecaoVisao.Visibility = Visibility.Collapsed;
                SecaoTesteAuditivo.Visibility = Visibility.Collapsed;
                SecaoExameGeral.Visibility = Visibility.Collapsed;
                SecaoHistoricoMedico.Visibility = Visibility.Collapsed;
                SecaoSaudeMental.Visibility = Visibility.Collapsed;
                TextoTituloFormulario.Text = _modoReavaliacaoMedica ? "Reavaliando a Pessoa" : "Confirmar Avaliação Médica";
                TextoDescricaoFormulario.Text = _modoReavaliacaoMedica
                    ? "Confira a ficha médica completa. Se precisar, altere os dados e salve a reavaliação."
                    : "Confira a ficha médica completa abaixo. Se precisar, altere qualquer campo antes de salvar.";
                TextoBotaoSalvar.Text = _modoReavaliacaoMedica ? "Salvar Reavaliação" : "Salvar Ficha";
                break;
        }
    }

    private void LimparBotao_Click(object sender, RoutedEventArgs e)
    {
        LimparControles(PainelFormularioSegundaEtapa);
        AtualizarCamposCondicionais();
        ListaResultadosPesquisaRA.ItemsSource = null;
        PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
        TextoResultadoPesquisaRA.Text = ObterTextoBuscaInicial();
        TextoFeedback.Text = string.Empty;
        TextoFeedback.Foreground = Brushes.Firebrick;
        _dadosAnaliseConfirmados = false;
        DefinirEtapaWizard(EtapaWizardIdentificacao);
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void PrepararProximoConscrito()
    {
        CaixaPesquisaRA.Text = string.Empty;
        CaixaTextoNome.Text = string.Empty;
        CaixaTextoCPF.Text = string.Empty;
        CaixaTextoRA.Text = string.Empty;
        SelecionarComboPorTexto(ComboResultadoAptidao, string.Empty);
        SelecionarComboPorTexto(ComboRestricaoAptidao, string.Empty);
        CaixaTextoProblemaMedico.Text = string.Empty;
        CaixaTextoCID.Text = string.Empty;
        ListaResultadosPesquisaRA.ItemsSource = null;
        PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
        TextoResultadoPesquisaRA.Text = $"Avaliado {_quantidadePessoasProcessadas} de {_quantidadePessoasAnalise}. Pesquise o proximo conscrito por nome ou RA.";
        TextoFeedback.Foreground = BordaEtapaAtiva;
        TextoFeedback.Text = $"Ficha salva. Falta(m) {_quantidadePessoasAnalise - _quantidadePessoasProcessadas} conscrito(s).";
        AtualizarCamposCondicionais();
        AtualizarWizardFormulario();
        ScrollFormularioSegundaEtapa.ScrollToHome();
    }

    /// <summary>
    /// Recarrega conscritos salvos e atualiza o total mostrado no menu lateral.
    /// </summary>
    private void CarregarResumo()
    {
        _conscritosCarregados = ServicoArmazenamentoConscritos.ObterTodos()
            .Where(ConscritoDeveAparecerNaBusca)
            .OrderBy(conscrito => conscrito.RA)
            .ThenBy(conscrito => conscrito.Nome)
            .ToList();

        TextoQuantidadeConscritos.Text = _conscritosCarregados.Count.ToString();
        AtualizarResultadosPesquisaRA();
    }

    private bool ConscritoDeveAparecerNaBusca(Conscrito conscrito)
    {
        if (conscrito.Faltoso)
        {
            return false;
        }

        return _modoReavaliacaoMedica
            ? conscrito.PrimeiraEtapaConcluida &&
              conscrito.SegundaEtapaConcluida &&
              conscrito.TerceiraEtapaConcluida &&
              !conscrito.QuartaEtapaConcluida
            : conscrito.PrimeiraEtapaConcluida &&
              !conscrito.SegundaEtapaConcluida;
    }

    private void CaixaPesquisaRA_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_atualizandoBuscaConscrito)
        {
            return;
        }

        var pesquisaContemLetras = CaixaPesquisaRA.Text.Any(char.IsLetter);
        var raFormatado = pesquisaContemLetras ? CaixaPesquisaRA.Text : FormatarRa(CaixaPesquisaRA.Text);
        if (!pesquisaContemLetras && CaixaPesquisaRA.Text != raFormatado)
        {
            _atualizandoBuscaConscrito = true;
            CaixaPesquisaRA.Text = raFormatado;
            CaixaPesquisaRA.CaretIndex = CaixaPesquisaRA.Text.Length;
            _atualizandoBuscaConscrito = false;
        }

        AtualizarResultadosPesquisaRA();
    }

    /// <summary>
    /// Filtra conscritos pelo RA digitado e mostra os resultados correspondentes.
    /// </summary>
    private void AtualizarResultadosPesquisaRA()
    {
        if (CaixaPesquisaRA is null)
        {
            return;
        }

        var pesquisa = CaixaPesquisaRA.Text.Trim();
        var digitosPesquisa = ObterApenasDigitos(pesquisa, 12);
        ListaResultadosPesquisaRA.SelectedItem = null;

        if (string.IsNullOrWhiteSpace(pesquisa))
        {
            ListaResultadosPesquisaRA.ItemsSource = null;
            PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
            TextoResultadoPesquisaRA.Text = ObterTextoBuscaInicial();
            return;
        }

        var resultados = _conscritosCarregados
            .Where(conscrito =>
                (!string.IsNullOrWhiteSpace(conscrito.RA) &&
                 (conscrito.RA.Contains(pesquisa, StringComparison.OrdinalIgnoreCase) ||
                  (!string.IsNullOrWhiteSpace(digitosPesquisa) &&
                   ObterApenasDigitos(conscrito.RA, 12).Contains(digitosPesquisa, StringComparison.OrdinalIgnoreCase)))) ||
                (!string.IsNullOrWhiteSpace(conscrito.Nome) &&
                 conscrito.Nome.Contains(pesquisa, StringComparison.OrdinalIgnoreCase)))
            .Take(8)
            .ToList();

        ListaResultadosPesquisaRA.ItemsSource = resultados;
        PainelResultadosPesquisaRA.Visibility = resultados.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TextoResultadoPesquisaRA.Text = resultados.Count > 0
            ? ObterTextoResultadoEncontrado(resultados.Count)
            : "Nenhum conscrito pendente encontrado para este nome ou RA.";
    }

    private void ListaResultadosPesquisaRA_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListaResultadosPesquisaRA.SelectedItem is not Conscrito conscrito)
        {
            return;
        }

        PreencherFormularioComConscrito(conscrito);
    }

    private bool TentarSelecionarConscritoPeloRaDigitado()
    {
        if (!string.IsNullOrWhiteSpace(CaixaTextoRA.Text))
        {
            return false;
        }

        var raPesquisado = CaixaPesquisaRA.Text.Trim();
        var digitosRaPesquisado = ObterApenasDigitos(raPesquisado, 12);
        if (string.IsNullOrWhiteSpace(raPesquisado))
        {
            return false;
        }

        var conscrito = _conscritosCarregados.FirstOrDefault(item =>
            string.Equals(item.RA, raPesquisado, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ObterApenasDigitos(item.RA, 12), digitosRaPesquisado, StringComparison.OrdinalIgnoreCase));

        if (conscrito is not null)
        {
            PreencherFormularioComConscrito(conscrito);
            return true;
        }

        return false;
    }

    private Conscrito? ObterConscritoSelecionadoOuPesquisado()
    {
        var ra = !string.IsNullOrWhiteSpace(CaixaTextoRA.Text)
            ? CaixaTextoRA.Text.Trim()
            : CaixaPesquisaRA.Text.Trim();
        var digitosRa = ObterApenasDigitos(ra, 12);

        if (string.IsNullOrWhiteSpace(ra))
        {
            return null;
        }

        return _conscritosCarregados.FirstOrDefault(item =>
            string.Equals(item.RA, ra, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ObterApenasDigitos(item.RA, 12), digitosRa, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Preenche identificacao e campos medicos ao selecionar um conscrito da busca.
    /// </summary>
    private void PreencherFormularioComConscrito(Conscrito conscrito)
    {
        var entrevistaMedica = ObterEntrevistaMedica(conscrito);

        _atualizandoBuscaConscrito = true;
        CaixaPesquisaRA.Text = conscrito.RA;
        _atualizandoBuscaConscrito = false;

        CaixaTextoNome.Text = conscrito.Nome;
        CaixaTextoCPF.Text = conscrito.CPF;
        CaixaTextoRA.Text = conscrito.RA;

        SelecionarComboPorTexto(ComboResultadoAptidao, entrevistaMedica.ResultadoAptidao);
        SelecionarComboPorTexto(ComboRestricaoAptidao, entrevistaMedica.Restricao);
        CaixaTextoProblemaMedico.Text = !string.IsNullOrWhiteSpace(entrevistaMedica.MotivoInaptidao)
            ? entrevistaMedica.MotivoInaptidao
            : entrevistaMedica.QualProblema;
        CaixaTextoCID.Text = entrevistaMedica.CID;
        AtualizarCamposCondicionais();

        if (!_modoReavaliacaoMedica)
        {
            LimparDadosMedicos();
            TextoResultadoPesquisaRA.Text = $"Selecionado: {conscrito.RA} - {conscrito.Nome}";
            TextoFeedback.Text = EntrevistaMedicaPossuiDados(entrevistaMedica)
                ? "Este conscrito já possui avaliação médica. Use a Quarta Etapa para visualizar ou editar a ficha."
                : string.Empty;
            ListaResultadosPesquisaRA.ItemsSource = null;
            PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
            return;
        }

        CaixaTextoAltura.Text = entrevistaMedica.Altura;
        CaixaTextoPeso.Text = entrevistaMedica.Peso;
        SelecionarComboPorTexto(ComboProblemaPostura, entrevistaMedica.ProblemaPostura);
        CaixaTextoObservacaoPostura.Text = entrevistaMedica.ObservacaoProblemaPostura;
        SelecionarComboPorTexto(ComboDificuldadeVisual, entrevistaMedica.DificuldadeVisualOuPrecisaOculos);
        SelecionarComboPorTexto(ComboTesteAuditivo, entrevistaMedica.TesteAuditivoAlterado);
        CaixaTextoObservacaoAuditiva.Text = entrevistaMedica.ObservacaoTesteAuditivo;
        CaixaTextoPressaoArterial.Text = entrevistaMedica.PressaoArterial;
        CaixaTextoFrequenciaCardiaca.Text = entrevistaMedica.FrequenciaCardiaca;
        CaixaTextoRespiracao.Text = entrevistaMedica.Respiracao;
        SelecionarComboPorTexto(ComboDoencasGravesFamilia, entrevistaMedica.FamiliaTemDoencasGraves);
        SelecionarComboPorTexto(ComboProblemaCardiacoRespiratorio, entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio);
        SelecionarComboPorTexto(ComboSaudeMental, entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico);
        SelecionarComboPorTexto(ComboDificuldadeDormir, entrevistaMedica.TemDificuldadeParaDormir);
        AtualizarCamposCondicionais();

        ListaResultadosPesquisaRA.ItemsSource = null;
        PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
        TextoResultadoPesquisaRA.Text = $"Ficha médica carregada: {conscrito.RA} - {conscrito.Nome}";
        TextoFeedback.Text = string.Empty;
        AtualizarWizardFormulario();
    }

    private string ObterTextoBuscaInicial()
    {
        return _modoReavaliacaoMedica
            ? "Digite o RA para localizar a ficha médica do conscrito."
            : "Digite o nome ou RA para localizar um conscrito pendente.";
    }

    private string ObterTextoResultadoEncontrado(int totalResultados)
    {
        return _modoReavaliacaoMedica
            ? $"{totalResultados} resultado(s) encontrado(s). Selecione um conscrito para abrir a ficha médica completa."
            : $"{totalResultados} resultado(s) encontrado(s). Selecione um conscrito para preencher a identificação.";
    }

    private void LimparDadosMedicos()
    {
        SelecionarComboPorTexto(ComboResultadoAptidao, string.Empty);
        SelecionarComboPorTexto(ComboRestricaoAptidao, string.Empty);
        CaixaTextoProblemaMedico.Text = string.Empty;
        CaixaTextoCID.Text = string.Empty;
        CaixaTextoAltura.Text = string.Empty;
        CaixaTextoPeso.Text = string.Empty;
        SelecionarComboPorTexto(ComboProblemaPostura, string.Empty);
        CaixaTextoObservacaoPostura.Text = string.Empty;
        SelecionarComboPorTexto(ComboDificuldadeVisual, string.Empty);
        SelecionarComboPorTexto(ComboTesteAuditivo, string.Empty);
        CaixaTextoObservacaoAuditiva.Text = string.Empty;
        CaixaTextoPressaoArterial.Text = string.Empty;
        CaixaTextoFrequenciaCardiaca.Text = string.Empty;
        CaixaTextoRespiracao.Text = string.Empty;
        SelecionarComboPorTexto(ComboDoencasGravesFamilia, string.Empty);
        SelecionarComboPorTexto(ComboProblemaCardiacoRespiratorio, string.Empty);
        SelecionarComboPorTexto(ComboSaudeMental, string.Empty);
        SelecionarComboPorTexto(ComboDificuldadeDormir, string.Empty);
        AtualizarCamposCondicionais();
    }

    private static string ObterNomeBlocoWizard(int etapa)
    {
        return etapa switch
        {
            EtapaWizardAvaliacaoFisica => "Avaliação física",
            EtapaWizardVisao => "Visão",
            EtapaWizardTesteAuditivo => "Teste auditivo",
            EtapaWizardExameGeral => "Exame geral",
            EtapaWizardHistoricoMedico => "Histórico médico",
            _ => "Bloco médico"
        };
    }

    private Border ObterSecaoBlocoWizard(int etapa)
    {
        return etapa switch
        {
            EtapaWizardAvaliacaoFisica => SecaoAvaliacaoFisica,
            EtapaWizardVisao => SecaoVisao,
            EtapaWizardTesteAuditivo => SecaoTesteAuditivo,
            EtapaWizardExameGeral => SecaoExameGeral,
            EtapaWizardHistoricoMedico => SecaoHistoricoMedico,
            _ => SecaoAvaliacaoFisica
        };
    }

    private void AtualizarIndicadorEtapa(Border indicador, int etapaIndicador)
    {
        var etapaAtiva = _etapaWizardAtual == etapaIndicador;
        var etapaConcluida = _etapaWizardAtual > etapaIndicador;
        AplicarVisualIndicadorEtapa(indicador, etapaAtiva, etapaConcluida);
    }

    private void AtualizarIndicadorEtapaPorIntervalo(Border indicador, int etapaInicial, int etapaFinal)
    {
        var etapaAtiva = _etapaWizardAtual >= etapaInicial && _etapaWizardAtual <= etapaFinal;
        var etapaConcluida = _etapaWizardAtual > etapaFinal;
        AplicarVisualIndicadorEtapa(indicador, etapaAtiva, etapaConcluida);
    }

    private static void AplicarVisualIndicadorEtapa(Border indicador, bool etapaAtiva, bool etapaConcluida)
    {
        indicador.Background = etapaAtiva
            ? FundoEtapaAtiva
            : etapaConcluida
                ? FundoEtapaConcluida
                : FundoEtapaInativa;
        indicador.BorderBrush = etapaAtiva
            ? BordaEtapaAtiva
            : etapaConcluida
                ? BordaEtapaConcluida
                : BordaEtapaInativa;
        indicador.BorderThickness = etapaAtiva ? new Thickness(2) : new Thickness(1);
    }

    /// <summary>
    /// Aplica mascara de CPF no campo de identificacao.
    /// </summary>
    private void CampoComMascara_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_atualizandoMascara || sender is not TextBox caixaTexto)
        {
            return;
        }

        var textoFormatado = caixaTexto == CaixaTextoCPF
            ? FormatarCpf(caixaTexto.Text)
            : caixaTexto.Text;

        if (caixaTexto.Text == textoFormatado)
        {
            return;
        }

        _atualizandoMascara = true;
        caixaTexto.Text = textoFormatado;
        caixaTexto.CaretIndex = caixaTexto.Text.Length;
        _atualizandoMascara = false;
    }

    private void CaixaTextoCRM_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraTexto(sender, FormatarCrm);
    }

    private void CaixaTextoCID_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraTexto(sender, FormatarCid);
    }

    private void AplicarMascaraTexto(object sender, Func<string, string> formatar)
    {
        if (_atualizandoMascara || sender is not TextBox caixaTexto)
        {
            return;
        }

        var textoFormatado = formatar(caixaTexto.Text);
        if (caixaTexto.Text == textoFormatado)
        {
            return;
        }

        _atualizandoMascara = true;
        caixaTexto.Text = textoFormatado;
        caixaTexto.CaretIndex = caixaTexto.Text.Length;
        _atualizandoMascara = false;
    }

    private static string FormatarCpf(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 11);

        return digitos.Length switch
        {
            > 9 => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}",
            > 6 => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..]}",
            > 3 => $"{digitos[..3]}.{digitos[3..]}",
            _ => digitos
        };
    }

    private static string FormatarCrm(string valor)
    {
        var digitos = string.Concat(valor.Where(char.IsDigit).Take(6));
        var letras = string.Concat(valor.Where(char.IsLetter).Take(2)).ToUpperInvariant();

        if (digitos.Length < 6)
        {
            return digitos;
        }

        return letras.Length > 0 ? $"{digitos}-{letras}" : digitos;
    }

    private static string FormatarCid(string valor)
    {
        var caracteres = string.Concat(valor
            .Where(char.IsLetterOrDigit)
            .Take(6))
            .ToUpperInvariant();

        if (caracteres.Length <= 3)
        {
            return caracteres;
        }

        return $"{caracteres[..3]}.{caracteres[3..]}";
    }

    private static bool CrmEstaValido(string valor)
    {
        var texto = valor.Trim();
        return texto.Length == 9 &&
               texto.Take(6).All(char.IsDigit) &&
               texto[6] == '-' &&
               texto.Skip(7).Take(2).All(char.IsLetter);
    }

    private static bool CidEstaValido(string valor)
    {
        var texto = valor.Trim().ToUpperInvariant();
        var cidBasicoValido = texto.Length >= 3 &&
                              char.IsLetter(texto[0]) &&
                              char.IsDigit(texto[1]) &&
                              char.IsDigit(texto[2]);

        if (!cidBasicoValido)
        {
            return false;
        }

        if (texto.Length == 3)
        {
            return true;
        }

        return texto.Length is >= 5 and <= 7 &&
               texto[3] == '.' &&
               texto.Skip(4).All(char.IsLetterOrDigit);
    }

    private static string FormatarRa(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 12);

        return digitos.Length switch
        {
            > 11 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}.{digitos[8..11]}-{digitos[11..]}",
            > 8 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}.{digitos[8..]}",
            > 5 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..]}",
            > 2 => $"{digitos[..2]}.{digitos[2..]}",
            _ => digitos
        };
    }

    private static string ObterApenasDigitos(string valor, int limite)
    {
        return string.Concat(valor.Where(char.IsDigit).Take(limite));
    }

    /// <summary>
    /// Copia os valores da tela para o objeto Conscrito antes de salvar.
    /// </summary>
    private void PreencherDadosMedicos(Conscrito conscrito)
    {
        var entrevistaMedica = ObterEntrevistaMedica(conscrito);

        entrevistaMedica.TipoAnalise = ObterTextoSelecionado(ComboTipoAnaliseMedica);
        entrevistaMedica.QuantidadePessoasAnalisadas = string.Equals(entrevistaMedica.TipoAnalise, "Grupal", StringComparison.OrdinalIgnoreCase)
            ? CaixaTextoQuantidadePessoas.Text.Trim()
            : "1";
        entrevistaMedica.CRM = CaixaTextoCRM.Text.Trim();
        entrevistaMedica.ResultadoAptidao = ObterTextoSelecionado(ComboResultadoAptidao);
        entrevistaMedica.Restricao = ObterTextoSelecionado(ComboRestricaoAptidao);
        entrevistaMedica.QualProblema = string.Equals(entrevistaMedica.ResultadoAptidao, "Apto com restrição", StringComparison.OrdinalIgnoreCase)
            ? CaixaTextoProblemaMedico.Text.Trim()
            : string.Empty;
        entrevistaMedica.MotivoInaptidao = string.Equals(entrevistaMedica.ResultadoAptidao, "Inapto", StringComparison.OrdinalIgnoreCase)
            ? CaixaTextoProblemaMedico.Text.Trim()
            : string.Empty;
        entrevistaMedica.CID = CaixaTextoCID.Text.Trim();

        entrevistaMedica.Altura = CaixaTextoAltura.Text.Trim();
        entrevistaMedica.Peso = CaixaTextoPeso.Text.Trim();
        entrevistaMedica.ProblemaPostura = ObterTextoSelecionado(ComboProblemaPostura);
        entrevistaMedica.ObservacaoProblemaPostura = CaixaTextoObservacaoPostura.Text.Trim();
        entrevistaMedica.DificuldadeVisualOuPrecisaOculos = ObterTextoSelecionado(ComboDificuldadeVisual);
        entrevistaMedica.TesteAuditivoAlterado = ObterTextoSelecionado(ComboTesteAuditivo);
        entrevistaMedica.ObservacaoTesteAuditivo = CaixaTextoObservacaoAuditiva.Text.Trim();
        entrevistaMedica.PressaoArterial = CaixaTextoPressaoArterial.Text.Trim();
        entrevistaMedica.FrequenciaCardiaca = CaixaTextoFrequenciaCardiaca.Text.Trim();
        entrevistaMedica.Respiracao = CaixaTextoRespiracao.Text.Trim();
        entrevistaMedica.FamiliaTemDoencasGraves = ObterTextoSelecionado(ComboDoencasGravesFamilia);
        entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio = ObterTextoSelecionado(ComboProblemaCardiacoRespiratorio);
        entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico = ObterTextoSelecionado(ComboSaudeMental);
        entrevistaMedica.TemDificuldadeParaDormir = ObterTextoSelecionado(ComboDificuldadeDormir);
    }

    private static EntrevistaMedica ObterEntrevistaMedica(Conscrito conscrito)
    {
        conscrito.Entrevista_Medica ??= new();
        return conscrito.Entrevista_Medica;
    }

    private static bool EntrevistaMedicaPossuiDados(EntrevistaMedica? entrevistaMedica)
    {
        return entrevistaMedica is not null &&
               (!string.IsNullOrWhiteSpace(entrevistaMedica.ResultadoAptidao) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.CRM) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.Altura) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.Peso) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.ProblemaPostura) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.ObservacaoProblemaPostura) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.DificuldadeVisualOuPrecisaOculos) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.TesteAuditivoAlterado) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.ObservacaoTesteAuditivo) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.PressaoArterial) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.FrequenciaCardiaca) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.Respiracao) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.FamiliaTemDoencasGraves) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.TemDificuldadeParaDormir));
    }

    private static string ObterTextoSelecionado(ComboBox? comboBox)
    {
        if (comboBox is null)
        {
            return string.Empty;
        }

        var texto = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;
        return texto == "Selecione" ? string.Empty : texto;
    }

    private static bool RespostaEhSim(string? valor)
    {
        return string.Equals(valor?.Trim(), "Sim", StringComparison.OrdinalIgnoreCase);
    }

    private static void SelecionarComboPorTexto(ComboBox comboBox, string valor)
    {
        for (var indice = 0; indice < comboBox.Items.Count; indice++)
        {
            if (comboBox.Items[indice] is ComboBoxItem item &&
                string.Equals(item.Content?.ToString(), valor, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedIndex = indice;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static void LimparControles(DependencyObject elemento)
    {
        if (elemento is TextBox caixaTexto)
        {
            caixaTexto.Text = string.Empty;
        }
        else if (elemento is ComboBox comboBox)
        {
            comboBox.SelectedIndex = 0;
        }
        else if (elemento is TextBlock textBlock && textBlock.Name == "TextoFeedback")
        {
            textBlock.Text = string.Empty;
        }

        var quantidadeFilhos = VisualTreeHelper.GetChildrenCount(elemento);
        for (var indice = 0; indice < quantidadeFilhos; indice++)
        {
            LimparControles(VisualTreeHelper.GetChild(elemento, indice));
        }
    }

    private void MostrarTelaInicial(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }
}
