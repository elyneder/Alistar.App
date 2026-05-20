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
    private const int EtapaWizardConfirmacao = 1;

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
    private readonly Conscrito? _conscritoInicial;
    private readonly bool _modoReavaliacaoMedica;
    private readonly bool _abrirEmContextoListaGeral;
    private List<Conscrito> _conscritosCarregados = [];
    private List<ItemAnaliseGrupo> _itensAnaliseGrupo = [];
    private bool _aguardandoConfirmacaoGrupoFinal;

    public TelaSegundaEtapa(Conscrito? conscrito = null, bool modoReavaliacaoMedica = false, bool abrirEmContextoListaGeral = false)
    {
        _conscritoInicial = conscrito;
        _modoReavaliacaoMedica = modoReavaliacaoMedica;
        _abrirEmContextoListaGeral = abrirEmContextoListaGeral;

        InitializeComponent();
        ComboTipoAnaliseMedica.SelectedIndex = 0;
        CaixaTextoQuantidadePessoas.Text = "1";
        AplicarModoTela();
        RegistrarEventosCamposCondicionais();
        AtualizarCamposCondicionais();
        CarregarResumo();
        DefinirEtapaWizard(EtapaWizardIdentificacao);
        CarregarConscritoInicial();

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

        Title = _abrirEmContextoListaGeral ? "Alistar | Lista Geral" : "Alistar | Quarta Etapa";
        TextoTituloPagina.Text = _abrirEmContextoListaGeral ? "Lista Geral" : "Quarta Etapa";
        TextoDescricaoPagina.Text = "Reavaliação médica do conscrito.";

        if (_abrirEmContextoListaGeral)
        {
            SelecionarComboPorTexto(ComboTipoAnaliseMedica, "Individual");
            CaixaTextoQuantidadePessoas.Text = "1";
        }
    }

    /// <summary>
    /// Liga perguntas Sim/Nao da avaliacao medica aos respectivos campos de observacao.
    /// </summary>
    private void RegistrarEventosCamposCondicionais()
    {
        ComboTipoAnaliseMedica.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboResultadoAptidao.SelectionChanged += CampoCondicional_SelectionChanged;
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
        var aptoComRestricao = ResultadoEhAptoComRestricao(resultado);
        var inapto = string.Equals(resultado, "Inapto", StringComparison.OrdinalIgnoreCase);
        DefinirVisibilidadeCondicional(aptoComRestricao, PainelRestricaoAptidao);
        DefinirVisibilidadeCondicional(aptoComRestricao || inapto, PainelProblemaMedico, PainelCidMedico);
        TextoRotuloProblemaMedico.Text = inapto ? "Motivo:" : "Qual problema:";

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

        if (_aguardandoConfirmacaoGrupoFinal)
        {
            SalvarAnaliseGrupoPendente();
            return;
        }

        if (_etapaWizardAtual == EtapaWizardIdentificacao && !_dadosAnaliseConfirmados)
        {
            if (!ValidarDadosAnalise())
            {
                return;
            }

            _dadosAnaliseConfirmados = true;
            _quantidadePessoasProcessadas = 0;
            PrepararItensAnaliseGrupo();
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

        if (AnaliseEhGrupal())
        {
            RegistrarResultadoGrupoAtual(nome, cpf, ra);
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

        ConcluirItemAnaliseAtual(conscrito);
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

        if (AnaliseEhGrupal())
        {
            ConcluirItemAnaliseAtual(conscrito, "Faltou");
            _quantidadePessoasProcessadas++;

            if (_quantidadePessoasProcessadas < _quantidadePessoasAnalise)
            {
                PrepararProximoConscrito();
                return;
            }

            _aguardandoConfirmacaoGrupoFinal = true;
            DefinirEtapaWizard(EtapaWizardConfirmacao);
            return;
        }

        conscrito.Faltoso = true;
        ConcluirItemAnaliseAtual(conscrito, "Faltou");
        _quantidadePessoasProcessadas++;
        ServicoArmazenamentoConscritos.Atualizar(conscrito);
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
        if (_modoReavaliacaoMedica && _abrirEmContextoListaGeral)
        {
            SelecionarComboPorTexto(ComboTipoAnaliseMedica, "Individual");
            CaixaTextoQuantidadePessoas.Text = "1";
            _quantidadePessoasAnalise = 1;
            return true;
        }

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
            if (quantidade > _conscritosCarregados.Count)
            {
                TextoFeedback.Text = $"Existem apenas {_conscritosCarregados.Count} conscrito(s) pendente(s) para esta etapa.";
                CaixaTextoQuantidadePessoas.Focus();
                return false;
            }

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

        if (!ValidarCidQuandoNecessario())
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
        return SecaoIdentificacao;
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
        if (_modoReavaliacaoMedica && _abrirEmContextoListaGeral)
        {
            RetornarParaListaGeralAnterior();
            return;
        }

        if (_etapaWizardAtual == EtapaWizardIdentificacao)
        {
            if (_dadosAnaliseConfirmados)
            {
                _dadosAnaliseConfirmados = false;
                _quantidadePessoasProcessadas = 0;
                _aguardandoConfirmacaoGrupoFinal = false;
                _itensAnaliseGrupo.Clear();
                AtualizarListaConscritosAnalise();
                CaixaPesquisaRA.Text = string.Empty;
                CaixaTextoNome.Text = string.Empty;
                CaixaTextoCPF.Text = string.Empty;
                CaixaTextoRA.Text = string.Empty;
                LimparDadosMedicos();
                AtualizarWizardFormulario();
            }

            return;
        }

        DefinirEtapaWizard(_etapaWizardAtual - 1);
    }

    private void DefinirEtapaWizard(int etapa)
    {
        _etapaWizardAtual = Math.Max(EtapaWizardIdentificacao, Math.Min(EtapaWizardConfirmacao, etapa));
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
        var mostrarPainelDadosAnalise = !_abrirEmContextoListaGeral && (mostrarDadosAnalise || mostrarFormularioCompleto);

        SecaoIdentificacao.Visibility = Visibility.Visible;
        PainelDadosAnalise.Visibility = mostrarPainelDadosAnalise ? Visibility.Visible : Visibility.Collapsed;
        PainelBuscaConscrito.Visibility = mostrarBuscaConscrito && !_aguardandoConfirmacaoGrupoFinal ? Visibility.Visible : Visibility.Collapsed;
        PainelConscritosAnalise.Visibility = _dadosAnaliseConfirmados && _quantidadePessoasAnalise > 1
            ? Visibility.Visible
            : Visibility.Collapsed;
        PainelDadosConscrito.Visibility = mostrarBuscaConscrito && !_aguardandoConfirmacaoGrupoFinal ? Visibility.Visible : Visibility.Collapsed;
        PainelAptidaoInicial.Visibility = mostrarBuscaConscrito && !_aguardandoConfirmacaoGrupoFinal ? Visibility.Visible : Visibility.Collapsed;

        AplicarVisualIndicadorEtapa(
            IndicadorEtapaIdentificacao,
            _etapaWizardAtual == EtapaWizardIdentificacao && !_dadosAnaliseConfirmados,
            _dadosAnaliseConfirmados || _etapaWizardAtual == EtapaWizardConfirmacao);
        AplicarVisualIndicadorEtapa(
            IndicadorEtapaExames,
            _etapaWizardAtual == EtapaWizardIdentificacao && _dadosAnaliseConfirmados,
            _etapaWizardAtual == EtapaWizardConfirmacao);
        AtualizarIndicadorEtapa(IndicadorEtapaConfirmacao, EtapaWizardConfirmacao);

        BotaoVoltarEtapaWizard.Visibility = _etapaWizardAtual > EtapaWizardIdentificacao || _dadosAnaliseConfirmados
            ? Visibility.Visible
            : Visibility.Collapsed;
        IconeAvancarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Collapsed : Visibility.Visible;
        IconeSalvarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        if (_etapaWizardAtual == EtapaWizardIdentificacao)
        {
            TextoTituloFormulario.Text = tituloFormulario;
            TextoDescricaoFormulario.Text = _dadosAnaliseConfirmados
                ? "Selecione o conscrito e informe apenas o resultado médico."
                : _modoReavaliacaoMedica
                    ? "Busque o RA do conscrito para abrir o resultado médico."
                    : "Defina o tipo de análise e depois selecione o conscrito para informar o resultado.";
            TextoBotaoSalvar.Text = "Próximo";
            return;
        }

        if (_aguardandoConfirmacaoGrupoFinal)
        {
            TextoTituloFormulario.Text = _modoReavaliacaoMedica ? "Confirmar Grupo da Quarta Etapa" : "Confirmar Grupo da Segunda Etapa";
            TextoDescricaoFormulario.Text = "Revise os conscritos e os resultados antes de salvar o grupo.";
            TextoBotaoSalvar.Text = "Salvar Grupo";
            return;
        }

        TextoTituloFormulario.Text = _modoReavaliacaoMedica ? "Reavaliando a Pessoa" : "Confirmar Resultado Médico";
        TextoDescricaoFormulario.Text = _modoReavaliacaoMedica
            ? "Confira o resultado médico. Se precisar, altere os dados e salve a reavaliação."
            : "Confira o resultado médico antes de salvar.";
        TextoBotaoSalvar.Text = _modoReavaliacaoMedica ? "Salvar Reavaliação" : "Salvar Resultado";
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
        _quantidadePessoasProcessadas = 0;
        _aguardandoConfirmacaoGrupoFinal = false;
        _itensAnaliseGrupo.Clear();
        AtualizarListaConscritosAnalise();
        DefinirEtapaWizard(EtapaWizardIdentificacao);
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void PrepararProximoConscrito()
    {
        _aguardandoConfirmacaoGrupoFinal = false;
        CaixaPesquisaRA.Text = string.Empty;
        CaixaTextoNome.Text = string.Empty;
        CaixaTextoCPF.Text = string.Empty;
        CaixaTextoRA.Text = string.Empty;
        LimparDadosMedicos();
        ListaResultadosPesquisaRA.ItemsSource = null;
        PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
        _etapaWizardAtual = EtapaWizardIdentificacao;
        _dadosAnaliseConfirmados = true;
        TextoResultadoPesquisaRA.Text = $"Avaliado {_quantidadePessoasProcessadas} de {_quantidadePessoasAnalise}. Pesquise o próximo conscrito por nome ou RA.";
        TextoFeedback.Foreground = BordaEtapaAtiva;
        TextoFeedback.Text = $"Ficha salva. Falta(m) {_quantidadePessoasAnalise - _quantidadePessoasProcessadas} conscrito(s).";
        AtualizarCamposCondicionais();
        AtualizarWizardFormulario();
        ScrollFormularioSegundaEtapa.ScrollToHome();
    }

    private void PrepararItensAnaliseGrupo()
    {
        _itensAnaliseGrupo = Enumerable.Range(1, _quantidadePessoasAnalise)
            .Select(indice => new ItemAnaliseGrupo
            {
                Indice = indice,
                Status = indice == 1 ? "Aguardando seleção" : "Pendente"
            })
            .ToList();

        AtualizarListaConscritosAnalise();
    }

    private void AtualizarItemAnaliseAtual(Conscrito conscrito)
    {
        var item = ObterItemAnaliseAtual();
        if (item is null)
        {
            return;
        }

        item.Nome = conscrito.Nome;
        item.CPF = conscrito.CPF;
        item.RA = conscrito.RA;
        item.Status = "Em preenchimento";
        AtualizarListaConscritosAnalise();
    }

    private void ConcluirItemAnaliseAtual(Conscrito conscrito, string status = "Avaliado")
    {
        var item = ObterItemAnaliseAtual();
        if (item is null)
        {
            return;
        }

        item.Nome = conscrito.Nome;
        item.CPF = conscrito.CPF;
        item.RA = conscrito.RA;
        item.Status = status;
        item.Faltou = string.Equals(status, "Faltou", StringComparison.OrdinalIgnoreCase);

        var proximo = _itensAnaliseGrupo.ElementAtOrDefault(_quantidadePessoasProcessadas + 1);
        if (proximo is not null && string.IsNullOrWhiteSpace(proximo.RA))
        {
            proximo.Status = "Aguardando seleção";
        }

        AtualizarListaConscritosAnalise();
    }

    private ItemAnaliseGrupo? ObterItemAnaliseAtual()
    {
        return _itensAnaliseGrupo.ElementAtOrDefault(_quantidadePessoasProcessadas);
    }

    private bool ConscritoJaEscolhidoNaAnalise(Conscrito conscrito)
    {
        if (!_dadosAnaliseConfirmados || _quantidadePessoasAnalise <= 1)
        {
            return false;
        }

        return _itensAnaliseGrupo
            .Where((_, indice) => indice != _quantidadePessoasProcessadas)
            .Any(item => !string.IsNullOrWhiteSpace(item.RA) &&
                         string.Equals(item.RA, conscrito.RA, StringComparison.OrdinalIgnoreCase));
    }

    private void AtualizarListaConscritosAnalise()
    {
        if (ListaConscritosAnalise is null)
        {
            return;
        }

        ListaConscritosAnalise.ItemsSource = null;
        ListaConscritosAnalise.ItemsSource = _itensAnaliseGrupo;
    }

    private bool AnaliseEhGrupal()
    {
        return _dadosAnaliseConfirmados && _quantidadePessoasAnalise > 1;
    }

    private void RegistrarResultadoGrupoAtual(string nome, string cpf, string ra)
    {
        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();
        var conscrito = conscritos.FirstOrDefault(item =>
            string.Equals(item.CPF, cpf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.RA, ra, StringComparison.OrdinalIgnoreCase));

        if (conscrito is null)
        {
            TextoFeedback.Text = "Localize um conscrito cadastrado pelo RA antes de confirmar o resultado.";
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        if (!_modoReavaliacaoMedica && EntrevistaMedicaPossuiDados(conscrito.Entrevista_Medica))
        {
            TextoFeedback.Text = "Este conscrito já possui avaliação médica. Use a Quarta Etapa para visualizar ou editar a ficha.";
            DefinirEtapaWizard(EtapaWizardIdentificacao);
            return;
        }

        var item = ObterItemAnaliseAtual();
        if (item is null)
        {
            return;
        }

        item.Nome = nome;
        item.CPF = cpf;
        item.RA = ra;
        item.Faltou = false;
        item.TipoAnalise = ObterTextoSelecionado(ComboTipoAnaliseMedica);
        item.QuantidadePessoasAnalisadas = CaixaTextoQuantidadePessoas.Text.Trim();
        item.CRM = CaixaTextoCRM.Text.Trim();
        item.Resultado = ObterTextoSelecionado(ComboResultadoAptidao);
        item.Restricao = ObterTextoSelecionado(ComboRestricaoAptidao);
        item.Problema = PainelProblemaMedico.Visibility == Visibility.Visible
            ? CaixaTextoProblemaMedico.Text.Trim()
            : string.Empty;
        item.CID = PainelCidMedico.Visibility == Visibility.Visible
            ? CaixaTextoCID.Text.Trim()
            : string.Empty;
        item.Status = item.ResumoResultado;
        AtualizarListaConscritosAnalise();

        _quantidadePessoasProcessadas++;

        if (_quantidadePessoasProcessadas < _quantidadePessoasAnalise)
        {
            PrepararProximoConscrito();
            return;
        }

        _aguardandoConfirmacaoGrupoFinal = true;
        DefinirEtapaWizard(EtapaWizardConfirmacao);
    }

    private void SalvarAnaliseGrupoPendente()
    {
        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();

        foreach (var item in _itensAnaliseGrupo)
        {
            var conscrito = conscritos.FirstOrDefault(registro =>
                string.Equals(registro.CPF, item.CPF, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(registro.RA, item.RA, StringComparison.OrdinalIgnoreCase));

            if (conscrito is null)
            {
                TextoFeedback.Text = $"Conscrito {item.RA} não encontrado para salvar.";
                return;
            }

            conscrito.Faltoso = item.Faltou;

            if (item.Faltou)
            {
                // Mantem a etapa pendente quando a pessoa faltou.
            }
            else if (_modoReavaliacaoMedica)
            {
                conscrito.QuartaEtapaConcluida = true;
            }
            else
            {
                conscrito.SegundaEtapaConcluida = true;
            }

            if (!item.Faltou)
            {
                PreencherDadosMedicos(conscrito, item);
            }

            try
            {
                ServicoArmazenamentoConscritos.Atualizar(conscrito);
            }
            catch (InvalidOperationException ex)
            {
                TextoFeedback.Text = ex.Message;
                return;
            }
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
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

        if (_conscritoInicial is not null &&
            string.Equals(conscrito.Id, _conscritoInicial.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _modoReavaliacaoMedica
            ? conscrito.PrimeiraEtapaConcluida &&
              conscrito.SegundaEtapaConcluida &&
              conscrito.TerceiraEtapaConcluida &&
              !conscrito.QuartaEtapaConcluida
            : conscrito.PrimeiraEtapaConcluida &&
              !conscrito.SegundaEtapaConcluida;
    }

    private void CarregarConscritoInicial()
    {
        if (_conscritoInicial is null)
        {
            return;
        }

        var conscritoAtualizado = _conscritosCarregados.FirstOrDefault(conscrito =>
            string.Equals(conscrito.Id, _conscritoInicial.Id, StringComparison.OrdinalIgnoreCase)) ?? _conscritoInicial;

        PreencherFormularioComConscrito(conscritoAtualizado);
        _dadosAnaliseConfirmados = true;
        DefinirEtapaWizard(EtapaWizardConfirmacao);
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
            .Where(conscrito => !ConscritoJaEscolhidoNaAnalise(conscrito))
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
            !ConscritoJaEscolhidoNaAnalise(item) &&
            (string.Equals(item.RA, raPesquisado, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(ObterApenasDigitos(item.RA, 12), digitosRaPesquisado, StringComparison.OrdinalIgnoreCase)));

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

        if (ConscritoJaEscolhidoNaAnalise(conscrito))
        {
            TextoFeedback.Foreground = Brushes.Firebrick;
            TextoFeedback.Text = "Este conscrito já foi selecionado nesta análise grupal.";
            return;
        }

        _atualizandoBuscaConscrito = true;
        CaixaPesquisaRA.Text = conscrito.RA;
        _atualizandoBuscaConscrito = false;

        CaixaTextoNome.Text = conscrito.Nome;
        CaixaTextoCPF.Text = conscrito.CPF;
        CaixaTextoRA.Text = conscrito.RA;
        SelecionarComboPorTexto(ComboTipoAnaliseMedica, string.IsNullOrWhiteSpace(entrevistaMedica.TipoAnalise) ? "Individual" : entrevistaMedica.TipoAnalise);
        CaixaTextoQuantidadePessoas.Text = string.IsNullOrWhiteSpace(entrevistaMedica.QuantidadePessoasAnalisadas)
            ? "1"
            : entrevistaMedica.QuantidadePessoasAnalisadas;
        CaixaTextoCRM.Text = entrevistaMedica.CRM;

        SelecionarComboPorTexto(ComboResultadoAptidao, entrevistaMedica.ResultadoAptidao);
        SelecionarComboPorTexto(ComboRestricaoAptidao, entrevistaMedica.Restricao);
        CaixaTextoProblemaMedico.Text = !string.IsNullOrWhiteSpace(entrevistaMedica.MotivoInaptidao)
            ? entrevistaMedica.MotivoInaptidao
            : entrevistaMedica.QualProblema;
        CaixaTextoCID.Text = entrevistaMedica.CID;
        AtualizarCamposCondicionais();
        AtualizarItemAnaliseAtual(conscrito);

        if (!_modoReavaliacaoMedica)
        {
            LimparDadosMedicos();
            AtualizarItemAnaliseAtual(conscrito);
            TextoResultadoPesquisaRA.Text = $"Selecionado: {conscrito.RA} - {conscrito.Nome}";
            TextoFeedback.Text = EntrevistaMedicaPossuiDados(entrevistaMedica)
                ? "Este conscrito já possui avaliação médica. Use a Quarta Etapa para visualizar ou editar a ficha."
                : string.Empty;
            ListaResultadosPesquisaRA.ItemsSource = null;
            PainelResultadosPesquisaRA.Visibility = Visibility.Collapsed;
            return;
        }

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
        AtualizarCamposCondicionais();
    }
    private void AtualizarIndicadorEtapa(Border indicador, int etapaIndicador)
    {
        var etapaAtiva = _etapaWizardAtual == etapaIndicador;
        var etapaConcluida = _etapaWizardAtual > etapaIndicador;
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
        var tipoAnalise = ObterTextoSelecionado(ComboTipoAnaliseMedica);
        var crmInformado = CaixaTextoCRM.Text.Trim();

        entrevistaMedica.TipoAnalise = !string.IsNullOrWhiteSpace(tipoAnalise)
            ? tipoAnalise
            : string.IsNullOrWhiteSpace(entrevistaMedica.TipoAnalise)
                ? "Individual"
                : entrevistaMedica.TipoAnalise;
        entrevistaMedica.QuantidadePessoasAnalisadas = string.Equals(entrevistaMedica.TipoAnalise, "Grupal", StringComparison.OrdinalIgnoreCase)
            ? CaixaTextoQuantidadePessoas.Text.Trim()
            : "1";
        entrevistaMedica.CRM = !string.IsNullOrWhiteSpace(crmInformado) || !_abrirEmContextoListaGeral
            ? crmInformado
            : entrevistaMedica.CRM;
        entrevistaMedica.ResultadoAptidao = ObterTextoSelecionado(ComboResultadoAptidao);
        entrevistaMedica.Restricao = ObterTextoSelecionado(ComboRestricaoAptidao);
        entrevistaMedica.QualProblema = ResultadoEhAptoComRestricao(entrevistaMedica.ResultadoAptidao)
            ? CaixaTextoProblemaMedico.Text.Trim()
            : string.Empty;
        entrevistaMedica.MotivoInaptidao = string.Equals(entrevistaMedica.ResultadoAptidao, "Inapto", StringComparison.OrdinalIgnoreCase)
            ? CaixaTextoProblemaMedico.Text.Trim()
            : string.Empty;
        entrevistaMedica.CID = CaixaTextoCID.Text.Trim();

        LimparCamposMedicosRemovidos(entrevistaMedica);
    }

    private void RetornarParaListaGeralAnterior()
    {
        var conscrito = ObterConscritoSelecionadoOuPesquisado();

        if (conscrito is null && _conscritoInicial is not null)
        {
            conscrito = ServicoArmazenamentoConscritos.ObterTodos().FirstOrDefault(item =>
                string.Equals(item.Id, _conscritoInicial.Id, StringComparison.OrdinalIgnoreCase)) ?? _conscritoInicial;
        }

        if (conscrito is not null)
        {
            ServicoNavegacao.Trocar(this, new TelaPrimeiraEtapa(conscrito, abrirEmContextoListaGeral: true));
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPrimeiraEtapa(abrirListaAoIniciar: true));
    }

    private static void PreencherDadosMedicos(Conscrito conscrito, ItemAnaliseGrupo item)
    {
        var entrevistaMedica = ObterEntrevistaMedica(conscrito);

        entrevistaMedica.TipoAnalise = item.TipoAnalise;
        entrevistaMedica.QuantidadePessoasAnalisadas = item.QuantidadePessoasAnalisadas;
        entrevistaMedica.CRM = item.CRM;
        entrevistaMedica.ResultadoAptidao = item.Resultado;
        entrevistaMedica.Restricao = item.Restricao;
        entrevistaMedica.QualProblema = ResultadoEhAptoComRestricao(item.Resultado)
            ? item.Problema
            : string.Empty;
        entrevistaMedica.MotivoInaptidao = string.Equals(item.Resultado, "Inapto", StringComparison.OrdinalIgnoreCase)
            ? item.Problema
            : string.Empty;
        entrevistaMedica.CID = item.CID;

        LimparCamposMedicosRemovidos(entrevistaMedica);
    }

    private static void LimparCamposMedicosRemovidos(EntrevistaMedica entrevistaMedica)
    {
        entrevistaMedica.Altura = string.Empty;
        entrevistaMedica.Peso = string.Empty;
        entrevistaMedica.ProblemaPostura = string.Empty;
        entrevistaMedica.ObservacaoProblemaPostura = string.Empty;
        entrevistaMedica.DificuldadeVisualOuPrecisaOculos = string.Empty;
        entrevistaMedica.TesteAuditivoAlterado = string.Empty;
        entrevistaMedica.ObservacaoTesteAuditivo = string.Empty;
        entrevistaMedica.PressaoArterial = string.Empty;
        entrevistaMedica.FrequenciaCardiaca = string.Empty;
        entrevistaMedica.Respiracao = string.Empty;
        entrevistaMedica.FamiliaTemDoencasGraves = string.Empty;
        entrevistaMedica.JaTeveProblemaCardiacoOuRespiratorio = string.Empty;
        entrevistaMedica.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico = string.Empty;
        entrevistaMedica.TemDificuldadeParaDormir = string.Empty;
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
                !string.IsNullOrWhiteSpace(entrevistaMedica.Restricao) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.QualProblema) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.MotivoInaptidao) ||
                !string.IsNullOrWhiteSpace(entrevistaMedica.CID));
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

    private static bool ResultadoEhAptoComRestricao(string? valor)
    {
        return string.Equals(valor?.Trim(), "Apto com restricao", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(valor?.Trim(), "Apto com restrição", StringComparison.OrdinalIgnoreCase);
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

public class ItemAnaliseGrupo
{
    public int Indice { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string CPF { get; set; } = string.Empty;

    public string RA { get; set; } = string.Empty;

    public string Status { get; set; } = "Pendente";

    public bool Faltou { get; set; }

    public string TipoAnalise { get; set; } = string.Empty;

    public string QuantidadePessoasAnalisadas { get; set; } = string.Empty;

    public string CRM { get; set; } = string.Empty;

    public string Resultado { get; set; } = string.Empty;

    public string Restricao { get; set; } = string.Empty;

    public string Problema { get; set; } = string.Empty;

    public string CID { get; set; } = string.Empty;

    public string Rotulo => $"Conscrito {Indice}";

    public string Identificacao => string.IsNullOrWhiteSpace(RA)
        ? "Nenhum conscrito selecionado"
        : $"{RA} - {Nome}";

    public string ResumoResultado
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Resultado))
            {
                return Status;
            }

            var partes = new[] { Resultado, Restricao, Problema, CID }
                .Where(valor => !string.IsNullOrWhiteSpace(valor));

            return string.Join(" | ", partes);
        }
    }
}
