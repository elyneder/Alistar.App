using System.Windows;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

/// <summary>
/// Tela da primeira etapa de selecao/cadastro do conscrito.
/// </summary>
/// <remarks>
/// Esta tela possui duas responsabilidades principais: cadastrar/editar a ficha
/// completa do conscrito e consultar a lista de conscritos cadastrados.
/// O formulario foi dividido em wizard para reduzir a quantidade de campos por tela.
/// Para apresentar: pense nela como a tela da ficha geral, antes da parte medica.
/// </remarks>
public partial class TelaPrimeiraEtapa : Window
{
    private const int AnoLimiteAlistamentoPadrao = 2008;

    // Indices usados para controlar qual parte do formulario aparece no wizard.
    private const int EtapaWizardInformacoesBasicas = 0;
    private const int EtapaWizardBlocoA = 1;
    private const int EtapaWizardBlocoB = 2;
    private const int EtapaWizardBlocoC = 3;
    private const int EtapaWizardBlocoD = 4;
    private const int EtapaWizardBlocoE = 5;
    private const int EtapaWizardBlocoG = 6;
    private const int EtapaWizardBlocoH = 7;
    private const int EtapaWizardBlocoI = 8;
    private const int EtapaWizardBlocoJ = 9;
    private const int EtapaWizardManifestacao = 10;
    private const int EtapaWizardConfirmacao = 11;
    private const int TotalEtapasWizard = 12;

    // Cores usadas para destacar a etapa atual e as etapas ja concluidas.
    private static readonly Brush FundoEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F5EA")!);
    private static readonly Brush FundoEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2FBF6")!);
    private static readonly Brush FundoEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")!);
    private static readonly Brush BordaEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#196743")!);
    private static readonly Brush BordaEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8CC7A3")!);
    private static readonly Brush BordaEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9E1DD")!);

    // Estado da tela: edicao, etapa atual, mascara de campos e lista carregada.
    private readonly Conscrito? _conscritoInicial;
    private readonly bool _abrirListaAoIniciar;
    private readonly bool _abrirEmContextoListaGeral;
    private readonly bool _modoEntrevistaTecnica;
    private string? _idConscritoEmEdicao;
    private int _etapaWizardAtual = EtapaWizardInformacoesBasicas;
    private bool _atualizandoMascara;
    private bool _emFluxoListaConscritos;
    private List<Conscrito> _conscritosCarregados = [];

    public TelaPrimeiraEtapa(Conscrito? conscrito = null, bool abrirListaAoIniciar = false, bool modoEntrevistaTecnica = false, bool abrirEmContextoListaGeral = false)
    {
        _conscritoInicial = conscrito;
        _abrirListaAoIniciar = abrirListaAoIniciar;
        _abrirEmContextoListaGeral = abrirEmContextoListaGeral;
        _modoEntrevistaTecnica = modoEntrevistaTecnica;
        _emFluxoListaConscritos = _abrirListaAoIniciar || _abrirEmContextoListaGeral;
        InitializeComponent();
        TextoDescricaoCabecalho.Text = _modoEntrevistaTecnica
    ? "Revisão e atualização da ficha da primeira etapa"
    : "Registro completo dos dados pessoais do conscrito";
        AtualizarTituloCabecalho();
        Title = _modoEntrevistaTecnica ? "Alistar | Entrevista Técnica" : Title;
        RegistrarEventosCamposCondicionais();
        AtualizarCamposCondicionais();
        BotaoMarcarFalta.Visibility = _modoEntrevistaTecnica ? Visibility.Visible : Visibility.Collapsed;
        CarregarConscritos();
        PrepararTela();

            VerEntrevistadores.Visibility = Visibility.Collapsed;
            VerEntrevistadores.IsEnabled = false;
            CadastrarEntrevistador.Visibility = Visibility.Collapsed;
            CadastrarEntrevistador.IsEnabled = false;
            BotaoListaConscritos.Visibility = Visibility.Collapsed;
            BotaoListaConscritos.IsEnabled = false;

    }

    private bool EmModoEdicao => !string.IsNullOrWhiteSpace(_idConscritoEmEdicao);
    private string NomeFluxoFormulario => _modoEntrevistaTecnica ? "Entrevista Técnica" : "Primeira Etapa de Seleção";
    private string RotuloAcaoSalvar => _modoEntrevistaTecnica ? "Salvar Entrevista" : "Salvar Ficha";

    private void AtualizarTituloCabecalho()
    {
        TextoTituloCabecalho.Text = _modoEntrevistaTecnica
            ? "Terceira Etapa"
            : _emFluxoListaConscritos
                ? "Lista Geral"
                : "Primeira Etapa";
    }

    private static int ObterAnoLimiteAlistamento()
    {
        var anoConfigurado = ObterConfiguracaoDoProcesso().AnoLimiteNascimento;
        return anoConfigurado > 0 ? anoConfigurado : AnoLimiteAlistamentoPadrao;
    }

    private static ConfiguracaoProcesso ObterConfiguracaoDoProcesso()
    {
        var configuracaoEmMemoria = ServicoConfiguracaoProcesso._configuracoes.LastOrDefault();
        if (configuracaoEmMemoria is not null)
        {
            return configuracaoEmMemoria;
        }

        var caminhoArquivo = AppDomain.CurrentDomain.BaseDirectory;
        var raizDoProjeto = Path.GetFullPath(Path.Combine(caminhoArquivo, @"..\..\..\"));
        var caminhoCompleto = Path.Combine(raizDoProjeto, "processo-config.json");

        if (!File.Exists(caminhoCompleto))
        {
            return ServicoConfiguracaoProcesso.Obter();
        }

        var conteudo = File.ReadAllText(caminhoCompleto);
        return JsonSerializer.Deserialize<ConfiguracaoProcesso>(conteudo) ?? ServicoConfiguracaoProcesso.Obter();
    }

    private Conscrito? ObterConscritoEmEdicao()
    {
        return string.IsNullOrWhiteSpace(_idConscritoEmEdicao)
            ? null
            : ServicoArmazenamentoConscritos.ObterTodos().FirstOrDefault(conscrito => conscrito.Id == _idConscritoEmEdicao);
    }

    /// <summary>
    /// Liga as perguntas de Sim/Nao aos campos que so fazem sentido quando a resposta e Sim.
    /// </summary>
    private void RegistrarEventosCamposCondicionais()
    {
        ComboPossuiFilhos.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboRecebeAuxilioGovernamental.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboTemCursosProfissionalizantes.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboExperienciaProfissional.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboPossuiCNH.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboCursoHabilitacao.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboPraticaEsportes.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboProblemaSaude.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboRemedioControlado.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboInternacaoPsiquiatrica.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboFuma.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboBebidaAlcoolica.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboExperimentouDrogas.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboAindaUsaDroga.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboParenteUsuarioDrogas.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboParenteTranstornoPsiquiatrico.SelectionChanged += CampoCondicional_SelectionChanged;
        ComboDetidoPelaPolicia.SelectionChanged += CampoCondicional_SelectionChanged;
    }

    private void CampoCondicional_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarCamposCondicionais();
    }

    /// <summary>
    /// Mostra detalhes somente para respostas afirmativas e deixa a validacao ignorar o que ficou oculto.
    /// </summary>
    private void AtualizarCamposCondicionais()
    {
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboPossuiFilhos)), [PainelQuantidadeFilhos]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboRecebeAuxilioGovernamental)), [PainelQualAuxilioGovernamental]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboTemCursosProfissionalizantes)), [PainelQuaisCursos, PainelComprovaCursos]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboExperienciaProfissional)), [PainelQuaisExperiencias, PainelComprovaExperiencia]);
        var possuiCnh = RespostaEhSim(ObterTextoSelecionado(ComboPossuiCNH));
        DefinirVisibilidadeCondicional(possuiCnh, [PainelCategoriaCNH]);

        if (possuiCnh)
        {
            SelecionarComboPorTexto(ComboCursoHabilitacao, "Não");
            PainelCursoHabilitacao.Visibility = Visibility.Collapsed;
            ComboCursoHabilitacao.IsEnabled = false;
        }
        else
        {
            PainelCursoHabilitacao.Visibility = Visibility.Visible;
            ComboCursoHabilitacao.IsEnabled = true;
        }
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboPraticaEsportes)), [PainelQuaisEsportes, PainelFederado]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboProblemaSaude)), [PainelQualProblemaSaude]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboRemedioControlado)), [PainelQualRemedioControlado, PainelDetalhesRemedioControlado]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboInternacaoPsiquiatrica)), [PainelDetalhesInternacao]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboFuma)), [PainelTempoFuma]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboBebidaAlcoolica)), [PainelFrequenciaBebida]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboExperimentouDrogas)), [PainelQualDroga, PainelAindaUsaDroga, PainelDetalhesUsoDroga, PainelUltimaVezDroga]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboAindaUsaDroga)), [PainelFrequenciaDroga]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboParenteUsuarioDrogas)), [PainelQuemParenteUsuarioDrogas, PainelImpactoParenteUsuarioDrogas]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboParenteTranstornoPsiquiatrico)), [PainelQuemParenteTranstornoPsiquiatrico, PainelImpactoTranstornoPsiquiatrico]);
        DefinirVisibilidadeCondicional(RespostaEhSim(ObterTextoSelecionado(ComboDetidoPelaPolicia)), [PainelQualInfracao, PainelOutrosAtosInfracionais]);
    }

    private static void DefinirVisibilidadeCondicional(bool mostrar, params UIElement[] elementos)
    {
        foreach (var elemento in elementos)
        {
            elemento.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;

            if (!mostrar)
            {
                LimparControles(elemento);
            }
        }
    }

    /// <summary>
    /// Decide se a tela inicia em novo cadastro, edicao ou lista de conscritos.
    /// </summary>
    private void PrepararTela()
    {
        PainelFiltrosRapidos.Visibility = Visibility.Collapsed;

        if (_conscritoInicial is not null)
        {
            CarregarConscritoParaEdicao(_conscritoInicial);
        }
        else if (_abrirListaAoIniciar)
        {
            MostrarListaConscritos();
        }
        else
        {
            PrepararNovoCadastro();
            MostrarCadastroConscrito();
        }
    }

    private void PrimeiraEtapaBotao_Click(object sender, RoutedEventArgs e)
    {
        _emFluxoListaConscritos = false;
        PrepararNovoCadastro();
        MostrarCadastroConscrito();
    }

    private void EtapaFuturaBotao_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Esta etapa ainda será desenvolvida em tela própria.", "Em desenvolvimento", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarListaConscritos();
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

    private void GradeConscritos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GradeConscritos.SelectedItem is not Conscrito conscritoSelecionado)
        {
            return;
        }

        CarregarConscritoParaEdicao(conscritoSelecionado);
        GradeConscritos.SelectedItem = null;
    }

    private void CaixaPesquisaConscrito_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        AplicarFiltrosLista();
        e.Handled = true;
    }

    private void PesquisarConscritoBotao_Click(object sender, RoutedEventArgs e)
    {
        AplicarFiltrosLista();
    }

    private void AlternarFiltrosBotao_Click(object sender, RoutedEventArgs e)
    {
        var filtrosVisiveis = PainelFiltrosRapidos.Visibility == Visibility.Visible;
        PainelFiltrosRapidos.Visibility = filtrosVisiveis ? Visibility.Collapsed : Visibility.Visible;
        TextoBotaoFiltros.Text = filtrosVisiveis ? "Filtros" : "Ocultar";
        IconeFiltros.Visibility = filtrosVisiveis ? Visibility.Visible : Visibility.Collapsed;
        IconeOcultar.Visibility = filtrosVisiveis ? Visibility.Collapsed : Visibility.Visible;
    }

    private void FiltroRapido_Changed(object sender, RoutedEventArgs e)
    {
        AplicarFiltrosLista();
    }

    private void LimparFiltrosBotao_Click(object sender, RoutedEventArgs e)
    {
        CaixaPesquisaConscrito.Text = string.Empty;

        FiltroSituacaoTG.IsChecked = false;
        FiltroSituacaoSubstituto.IsChecked = false;
        FiltroSituacaoApto.IsChecked = false;
        FiltroSituacaoInapto.IsChecked = false;
        FiltroSituacaoDispensado.IsChecked = false;
        FiltroSituacaoRefratario.IsChecked = false;
        FiltroSituacaoIndefinido.IsChecked = false;
        FiltroAndamentoEmAndamento.IsChecked = false;
        FiltroAndamentoFinalizado.IsChecked = false;
        FiltroAndamentoFaltoso.IsChecked = false;
        FiltroTrabalha.IsChecked = false;
        FiltroRecebeAuxilio.IsChecked = false;
        FiltroEstuda.IsChecked = false;
        FiltroExperiencia.IsChecked = false;
        FiltroProblemaSaude.IsChecked = false;
        FiltroDesejaServirSim.IsChecked = false;
        FiltroDesejaServirNao.IsChecked = false;

        AplicarFiltrosLista();
    }

    /// <summary>
    /// Botao principal do formulario: avanca no wizard ou salva na confirmacao.
    /// </summary>
    private void SalvarConscritoBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        var conscrito = MontarConscritoPeloFormulario();

        if (_etapaWizardAtual != EtapaWizardConfirmacao)
        {    
            if (!ValidarEtapaAtual(conscrito))
            {
                if (string.IsNullOrWhiteSpace(TextoFeedbackCadastroConscrito.Text))
                {
                    TextoFeedbackCadastroConscrito.Text = "Preencha todos os campos desta etapa antes de continuar.";
                }

                return;
            }

            AvancarEtapaWizard();
            return;
        }

        if (!ValidarFichaCompleta(conscrito))
        {
            if (string.IsNullOrWhiteSpace(TextoFeedbackCadastroConscrito.Text))
            {
                TextoFeedbackCadastroConscrito.Text = "Preencha todos os campos da ficha antes de salvar.";
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(conscrito.Situacao))
        {
            conscrito.Situacao = "Indefinido";
        }

        try
        {
            if (EmModoEdicao)
            {
                AplicarAndamentoDaEtapa(conscrito);
                ServicoArmazenamentoConscritos.Atualizar(conscrito);
            }
            else
            {
                AplicarAndamentoDaEtapa(conscrito);
                ServicoArmazenamentoConscritos.Adicionar(conscrito);
            }
        }
        catch (InvalidOperationException ex)
        {
            TextoFeedbackCadastroConscrito.Text = ex.Message;
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return;
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    private void LimparFormularioBotao_Click(object sender, RoutedEventArgs e)
    {
        if (EmModoEdicao)
        {
            ServicoNavegacao.Trocar(this, new TelaPainelControle());
            return;
        }

        LimparCamposFormulario();
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
    }

    private void MarcarFaltaBotao_Click(object sender, RoutedEventArgs e)
    {
        var conscrito = ObterConscritoEmEdicao();
        if (conscrito is null)
        {
            TextoFeedbackCadastroConscrito.Text = "Selecione um conscrito da lista antes de marcar falta.";
            MostrarListaConscritos();
            return;
        }

        conscrito.Faltoso = true;
        ServicoArmazenamentoConscritos.Atualizar(conscrito);
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }


    private void GerarPdfCadastroBotao_Click(object sender, RoutedEventArgs e)
    {
        // O PDF usa o registro salvo no JSON, nao apenas o que esta digitado na tela.
        // Assim evitamos gerar relatorio com dados ainda nao salvos.
        var conscrito = ObterConscritoEmEdicao();
        if (conscrito is null)
        {
            return;
        }

        if (ServicoRelatorioPdf.GerarRelatorioCadastro(conscrito))
        {
            MessageBox.Show("Relatório de cadastro gerado com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void GerarPdfMedicoBotao_Click(object sender, RoutedEventArgs e)
    {
        var conscrito = ObterConscritoEmEdicao();
        if (conscrito is null)
        {
            return;
        }

        if (!ServicoRelatorioPdf.PossuiRelatorioMedico(conscrito))
        {
            MessageBox.Show("Este conscrito ainda não possui avaliação médica salva.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (ServicoRelatorioPdf.GerarRelatorioMedico(conscrito))
        {
            MessageBox.Show("Relatório médico gerado com sucesso.", "Alistar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AbrirEtapaMedicaBotao_Click(object sender, RoutedEventArgs e)
    {
        var conscrito = ObterConscritoEmEdicao();
        if (conscrito is null)
        {
            TextoFeedbackCadastroConscrito.Text = "Selecione um conscrito antes de abrir a etapa médica.";
            return;
        }

        ServicoAuditoria.RegistrarAcao("Acesso", "Etapa Médica", $"Usuário abriu a etapa médica de {conscrito.Nome} pela lista de candidatos.");
        ServicoNavegacao.Trocar(this, new TelaSegundaEtapa(conscrito, modoReavaliacaoMedica: true, abrirEmContextoListaGeral: _emFluxoListaConscritos));
    }

    private void CarregarConscritos()
    {
        // Sempre que a lista abre, recarregamos do JSON para pegar cadastros recentes.
        var consulta = ServicoArmazenamentoConscritos.ObterTodos().AsEnumerable();
        if (_modoEntrevistaTecnica)
        {
            consulta = consulta.Where(conscrito =>
                conscrito.PrimeiraEtapaConcluida &&
                conscrito.SegundaEtapaConcluida &&
                !conscrito.TerceiraEtapaConcluida &&
                !conscrito.Faltoso);
        }

        _conscritosCarregados = consulta
            .OrderBy(conscrito => conscrito.Nome)
            .ToList();

        TextoQuantidadeConscritos.Text = _conscritosCarregados.Count.ToString();
        AplicarFiltrosLista();
    }

    private void MostrarCadastroConscrito()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoCadastroConscrito.Visibility = Visibility.Visible;
        VisaoListaConscritos.Visibility = Visibility.Collapsed;
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        AtualizarTituloCabecalho();
        AtualizarWizardFormulario();
        ScrollFormularioCadastro.ScrollToHome();
    }

    private void MostrarListaConscritos()
    {
        _emFluxoListaConscritos = true;
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoCadastroConscrito.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Visible;
        GradeConscritos.SelectedItem = null;
        AtualizarTituloCabecalho();
        CarregarConscritos();
    }

    private void PrepararNovoCadastro()
    {
        _emFluxoListaConscritos = false;
        _idConscritoEmEdicao = null;
        LimparCamposFormulario();
        ComboSituacaoConscrito.SelectedIndex = 0;
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
    }

    /// <summary>
    /// Carrega um conscrito existente para edicao, preenchendo todos os campos.
    /// </summary>
    private void CarregarConscritoParaEdicao(Conscrito conscrito)
    {
        var veioDaListaConscritos = _emFluxoListaConscritos || VisaoListaConscritos.Visibility == Visibility.Visible;

        // Ao clicar em alguem da lista, copiamos os dados do objeto para os campos da tela.
        // O Id fica guardado para o salvar saber que e edicao, nao novo cadastro.
        _emFluxoListaConscritos = veioDaListaConscritos;
        _idConscritoEmEdicao = conscrito.Id;
        LimparCamposFormulario();
        TextoFeedbackCadastroConscrito.Text = string.Empty;

        CaixaTextoNomeConscrito.Text = conscrito.Nome;
        CaixaTextoCPF.Text = conscrito.CPF;
        CaixaTextoRA.Text = conscrito.RA;
        CaixaTextoNomeMae.Text = conscrito.NomeMae;
        CaixaTextoDataNascimento.Text = conscrito.DataNascimento;
        CaixaTextoPaisResidencia.Text = conscrito.PaisResidencia;
        CaixaTextoMunicipioResidencia.Text = conscrito.MunicipioResidencia;
        SelecionarComboPorTexto(ComboZonaResidencia, conscrito.ZonaResidencia);
        CaixaTextoPeso.Text = conscrito.Peso;
        CaixaTextoAltura.Text = conscrito.Altura;
        CaixaTextoTamanhoCabeca.Text = conscrito.TamanhoCabeca;
        CaixaTextoTamanhoCalcado.Text = conscrito.TamanhoCalcado;
        SelecionarComboPorTexto(ComboSituacaoConscrito, conscrito.Situacao, "Indefinido");
        CaixaTextoEndereco.Text = conscrito.Entrevista_Vida_Pessoal.Endereco;
        CaixaTextoBairro.Text = conscrito.Entrevista_Vida_Pessoal.Bairro;
        CaixaTextoCEP.Text = conscrito.Entrevista_Vida_Pessoal.CEP;
        CaixaTextoTelefone.Text = conscrito.Entrevista_Vida_Pessoal.Telefone;
        CaixaTextoEmail.Text = conscrito.Entrevista_Vida_Pessoal.Email;
        CaixaTextoOcupacao.Text = conscrito.Entrevista_Vida_Pessoal.Ocupacao;
        CaixaTextoMoraCom.Text = conscrito.Entrevista_Vida_Pessoal.MoraCom;
        SelecionarComboPorTexto(ComboEstadoCivil, conscrito.Entrevista_Vida_Pessoal.EstadoCivil);
        SelecionarComboPorTexto(ComboPossuiFilhos, conscrito.Entrevista_Vida_Pessoal.PossuiFilhos);
        CaixaTextoQuantidadeFilhos.Text = conscrito.Entrevista_Vida_Pessoal.QuantidadeFilhos;
        CaixaTextoQuemTrabalhaFamilia.Text = conscrito.Entrevista_Vida_Pessoal.QuemTrabalhaNaFamilia;
        CaixaTextoQuemSustentaFamilia.Text = conscrito.Entrevista_Vida_Pessoal.QuemSustentaAFamilia;
        SelecionarComboPorTexto(ComboRecebeAuxilioGovernamental, conscrito.Entrevista_Vida_Pessoal.RecebeAuxilioGovernamental);
        CaixaTextoQualAuxilioGovernamental.Text = conscrito.Entrevista_Vida_Pessoal.QualAuxilioGovernamental;
        SelecionarComboPorTexto(ComboSituacaoArrimo, conscrito.Entrevista_Arrimo_De_Familia.SituacaoArrimo);
        SelecionarComboPorTexto(ComboEstudaAtualmente, conscrito.Entrevista_Arrimo_De_Familia.EstudaAtualmente);
        CaixaTextoAnoEscolar.Text = conscrito.Entrevista_Arrimo_De_Familia.AnoQueEstaCursandoOuUltimoAnoQueCursou;
        SelecionarComboPorTexto(ComboTemCursosProfissionalizantes, conscrito.Entrevista_Cursos.TemCursosProfissionalizantes);
        CaixaTextoQuaisCursos.Text = conscrito.Entrevista_Cursos.QuaisCursosProfissionalizantes;
        SelecionarComboPorTexto(ComboComprovaCursos, conscrito.Entrevista_Cursos.ComprovaCursosProfissionalizantes);
        SelecionarComboPorTexto(ComboExperienciaProfissional, conscrito.Entrevista_Experiencia.ExperienciaProfissional);
        CaixaTextoQuaisExperiencias.Text = conscrito.Entrevista_Experiencia.QuaisExperienciasProfissionais;
        SelecionarComboPorTexto(ComboComprovaExperiencia, conscrito.Entrevista_Experiencia.ComprovaExperienciaProfissional);
        SelecionarComboPorTexto(ComboPossuiCNH, conscrito.Entrevista_Habilitacao.PossuiCNH);
        SelecionarComboPorTexto(ComboCursoHabilitacao, conscrito.Entrevista_Habilitacao.RealizandoCursoParaHabilitacao);
        CaixaTextoCategoriaCNH.Text = conscrito.Entrevista_Habilitacao.CategoriaCNH;
        SelecionarComboPorTexto(ComboPraticaEsportes, conscrito.Entrevista_Esportes.PraticaEsportes);
        CaixaTextoQuaisEsportes.Text = conscrito.Entrevista_Esportes.QuaisEsportes;
        SelecionarComboPorTexto(ComboFederado, conscrito.Entrevista_Esportes.EhOuJaFoiFederado);
        SelecionarComboPorTexto(ComboSabeNadar, conscrito.Entrevista_Esportes.SabeNadar);
        CaixaTextoLazer.Text = conscrito.Entrevista_Lazer.OQueFazNasHorasDeLazer;
        SelecionarComboPorTexto(ComboProblemaSaude, conscrito.Entrevista_Saude.JaTeveProblemaSaude);
        CaixaTextoQualProblemaSaude.Text = conscrito.Entrevista_Saude.QualProblemaSaude;
        SelecionarComboPorTexto(ComboRemedioControlado, conscrito.Entrevista_Saude.UsaRemedioControlado);
        CaixaTextoQualRemedioControlado.Text = conscrito.Entrevista_Saude.QualRemedioControlado;
        CaixaTextoParaQueRemedio.Text = conscrito.Entrevista_Saude.ParaQueUsaRemedioControlado;
        CaixaTextoHaQuantoTempoRemedio.Text = conscrito.Entrevista_Saude.HaQuantoTempoUsaRemedioControlado;
        CaixaTextoTempoRestanteRemedio.Text = conscrito.Entrevista_Saude.PorQuantoTempoAindaUsaraRemedio;
        SelecionarComboPorTexto(ComboInternacaoPsiquiatrica, conscrito.Entrevista_Saude.JaEsteveInternadoHospitalOuClinicaPsiquiatrica);
        CaixaTextoMotivoInternacao.Text = conscrito.Entrevista_Saude.MotivoInternacao;
        CaixaTextoTempoInternacao.Text = conscrito.Entrevista_Saude.TempoInternacao;
        SelecionarComboPorTexto(ComboFuma, conscrito.Entrevista_Saude.Fuma);
        CaixaTextoTempoFuma.Text = conscrito.Entrevista_Saude.HaQuantoTempoFuma;
        SelecionarComboPorTexto(ComboBebidaAlcoolica, conscrito.Entrevista_Saude.FazUsoBebidaAlcoolica);
        CaixaTextoFrequenciaBebida.Text = conscrito.Entrevista_Saude.FrequenciaBebidaAlcoolica;
        SelecionarComboPorTexto(ComboExperimentouDrogas, conscrito.Entrevista_Saude.JaExperimentouDrogas);
        CaixaTextoQualDroga.Text = conscrito.Entrevista_Saude.QualDroga;
        SelecionarComboPorTexto(ComboAindaUsaDroga, conscrito.Entrevista_Saude.AindaFazUsoDroga);
        CaixaTextoFrequenciaDroga.Text = conscrito.Entrevista_Saude.FrequenciaUsoDroga;
        CaixaTextoUltimaVezDroga.Text = conscrito.Entrevista_Saude.QuandoFoiUltimaVezQueUtilizouDroga;
        SelecionarComboPorTexto(ComboParenteUsuarioDrogas, conscrito.Entrevista_Saude.PossuiParenteUsuarioDrogas);
        CaixaTextoQuemParenteUsuarioDrogas.Text = conscrito.Entrevista_Saude.QuemParenteUsuarioDrogas;
        CaixaTextoImpactoParenteUsuarioDrogas.Text = conscrito.Entrevista_Saude.ComoParenteUsuarioDrogasAfetaSuaVida;
        SelecionarComboPorTexto(ComboParenteTranstornoPsiquiatrico, conscrito.Entrevista_Saude.PossuiParenteComHistoricoTranstornoPsiquiatrico);
        CaixaTextoQuemParenteTranstornoPsiquiatrico.Text = conscrito.Entrevista_Saude.QuemParenteComHistoricoTranstornoPsiquiatrico;
        CaixaTextoImpactoTranstornoPsiquiatrico.Text = conscrito.Entrevista_Saude.ComoTranstornoPsiquiatricoAfetaSuaVida;
        SelecionarComboPorTexto(ComboDetidoPelaPolicia, conscrito.Entrevista_Infracao.JaFoiDetidoPelaPolicia);
        CaixaTextoQualInfracao.Text = conscrito.Entrevista_Infracao.QualFoiAInfracao;
        CaixaTextoOutrosAtosInfracionais.Text = conscrito.Entrevista_Infracao.OutrosAtosInfracionais;
        SelecionarComboPorTexto(ComboDesejaServir, conscrito.DesejaServir);
        CaixaTextoObservacaoManifestacao.Text = conscrito.Observacao;
        AtualizarCamposCondicionais();

        MostrarCadastroConscrito();
        DefinirEtapaWizard(_modoEntrevistaTecnica ? EtapaWizardInformacoesBasicas : EtapaWizardConfirmacao);
        AtualizarTituloCabecalho();
    }

    private void VoltarEtapaWizardBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_etapaWizardAtual == EtapaWizardInformacoesBasicas)
        {
            return;
        }

        DefinirEtapaWizard(_etapaWizardAtual - 1);
    }

    /// <summary>
    /// Define a etapa atual do wizard e atualiza a interface.
    /// </summary>
    private void DefinirEtapaWizard(int etapa)
    {
        _etapaWizardAtual = Math.Max(EtapaWizardInformacoesBasicas, Math.Min(EtapaWizardConfirmacao, etapa));
        AtualizarWizardFormulario();
        ScrollFormularioCadastro.ScrollToHome();
    }

    private void AvancarEtapaWizard()
    {
        DefinirEtapaWizard(_etapaWizardAtual + 1);
    }

    /// <summary>
    /// Mostra somente a secao da etapa atual. Na confirmacao, mostra o formulario completo.
    /// </summary>
    private void AtualizarWizardFormulario()
    {
        var mostrarFormularioCompleto = _etapaWizardAtual == EtapaWizardConfirmacao;
        var mostrarQuestionario = (_etapaWizardAtual >= EtapaWizardBlocoA && _etapaWizardAtual <= EtapaWizardBlocoJ) || mostrarFormularioCompleto;

        SecaoInformacoesBasicas.Visibility = (_etapaWizardAtual == EtapaWizardInformacoesBasicas || mostrarFormularioCompleto)
            ? Visibility.Visible
            : Visibility.Collapsed;
        PainelSecoesQuestionario.Visibility = mostrarQuestionario ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoA.Visibility = (_etapaWizardAtual == EtapaWizardBlocoA || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoB.Visibility = (_etapaWizardAtual == EtapaWizardBlocoB || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoC.Visibility = (_etapaWizardAtual == EtapaWizardBlocoC || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoD.Visibility = (_etapaWizardAtual == EtapaWizardBlocoD || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoE.Visibility = (_etapaWizardAtual == EtapaWizardBlocoE || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoG.Visibility = (_etapaWizardAtual == EtapaWizardBlocoG || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoH.Visibility = (_etapaWizardAtual == EtapaWizardBlocoH || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoI.Visibility = (_etapaWizardAtual == EtapaWizardBlocoI || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoBlocoJ.Visibility = (_etapaWizardAtual == EtapaWizardBlocoJ || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoManifestacaoDesejoServir.Visibility = (_etapaWizardAtual == EtapaWizardManifestacao || mostrarFormularioCompleto)
            ? Visibility.Visible
            : Visibility.Collapsed;

        AtualizarIndicadorEtapa(IndicadorEtapaBasica, EtapaWizardInformacoesBasicas);
        AtualizarIndicadorEtapaPorIntervalo(IndicadorEtapaQuestionario, EtapaWizardBlocoA, EtapaWizardBlocoJ);
        AtualizarIndicadorEtapa(IndicadorEtapaManifestacao, EtapaWizardManifestacao);
        AtualizarIndicadorEtapa(IndicadorEtapaConfirmacao, EtapaWizardConfirmacao);

        BotaoVoltarEtapaWizard.Visibility = _etapaWizardAtual == EtapaWizardInformacoesBasicas
            ? Visibility.Collapsed
            : Visibility.Visible;
        BotaoPdfCadastro.Visibility = EmModoEdicao ? Visibility.Visible : Visibility.Collapsed;
        BotaoPdfMedico.Visibility = EmModoEdicao && ObterConscritoEmEdicao() is { } conscrito && ServicoRelatorioPdf.PossuiRelatorioMedico(conscrito)
            ? Visibility.Visible
            : Visibility.Collapsed;
        BotaoAbrirEtapaMedica.Visibility = EmModoEdicao ? Visibility.Visible : Visibility.Collapsed;
        TextoBotaoLimpar.Text = EmModoEdicao ? "Fechar" : "Limpar";
        IconeAvancarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Collapsed : Visibility.Visible;
        IconeSalvarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        switch (_etapaWizardAtual)
        {
            case EtapaWizardInformacoesBasicas:
                if (_modoEntrevistaTecnica)
                {
                    TextoTituloFormulario.Text = "Terceira Etapa";
                    TextoDescricaoFormulario.Text = "Testes de aptidão";
                }
                else
                {
                    TextoTituloFormulario.Text = EmModoEdicao ? "Editar Conscrito" : "Informações Gerais do Conscrito";
                    TextoDescricaoFormulario.Text = "Registro completo de dados pessoais e perfil";
                }
                
                TextoEtapaWizard.Text = $"Etapa 1 de {TotalEtapasWizard} · Informações básicas";
                TextoResumoEtapaWizard.Text = "Preencha os dados essenciais do conscrito para iniciar o cadastro.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case >= EtapaWizardBlocoA and <= EtapaWizardBlocoJ:
                var nomeBloco = ObterNomeBlocoWizard(_etapaWizardAtual);
                TextoTituloFormulario.Text = EmModoEdicao ? (_modoEntrevistaTecnica ? "Entrevista Técnica" : "Editar Conscrito") : NomeFluxoFormulario;
                TextoDescricaoFormulario.Text = "Preencha este bloco e avance pela seta para continuar o formulário em partes menores.";
                TextoEtapaWizard.Text = $"Etapa {_etapaWizardAtual + 1} de {TotalEtapasWizard} · {nomeBloco}";
                TextoResumoEtapaWizard.Text = "Ao avançar, o próximo bloco será exibido separadamente. No final, tudo aparecerá junto para conferência.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case EtapaWizardManifestacao:
                TextoTituloFormulario.Text = EmModoEdicao ? (_modoEntrevistaTecnica ? "Entrevista Técnica" : "Editar Conscrito") : NomeFluxoFormulario;
                TextoDescricaoFormulario.Text = "Preencha o bloco J e depois avance para revisar o formulário completo antes de salvar.";
                TextoEtapaWizard.Text = $"Etapa 11 de {TotalEtapasWizard} · Bloco J";
                TextoResumoEtapaWizard.Text = "Registre a situação, a manifestação do desejo de servir e a observação para concluir o preenchimento.";
                TextoBotaoSalvar.Text = "Ir para confirmação";
                break;

            default:
                TextoTituloFormulario.Text = _modoEntrevistaTecnica ? "Confirmar Entrevista Técnica" : EmModoEdicao ? "Detalhes do Conscrito" : "Confirmar Dados do Conscrito";
                TextoDescricaoFormulario.Text = "Confira o formulário completo abaixo. Se precisar, altere qualquer campo antes de salvar a ficha.";
                TextoEtapaWizard.Text = $"Etapa 12 de {TotalEtapasWizard} · Confirmação final";
                TextoResumoEtapaWizard.Text = "Toda a ficha aparece completa para revisão e ajustes finais antes do salvamento.";
                TextoBotaoSalvar.Text = EmModoEdicao ? (_modoEntrevistaTecnica ? RotuloAcaoSalvar : "Salvar Alterações") : RotuloAcaoSalvar;
                break;
        }
    }

    private static string ObterNomeBlocoWizard(int etapa)
    {
        return etapa switch
        {
            EtapaWizardBlocoA => "Bloco A · Vida pessoal",
            EtapaWizardBlocoB => "Bloco B · Arrimo de família",
            EtapaWizardBlocoC => "Bloco C · Cursos",
            EtapaWizardBlocoD => "Bloco D · Experiência",
            EtapaWizardBlocoE => "Bloco E · Habilitação",
            EtapaWizardBlocoG => "Bloco F · Prática de esportes",
            EtapaWizardBlocoH => "Bloco G · Lazer",
            EtapaWizardBlocoI => "Bloco H · Saúde",
            EtapaWizardBlocoJ => "Bloco I · Ato infracional",
            _ => "Bloco"
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

    /// <summary>
    /// Aplica o visual de ativo/concluido/inativo em cada card do wizard.
    /// </summary>
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
    /// Aplica mascara e filtros nos campos com formato definido enquanto o usuario digita.
    /// </summary>
    private void CampoComMascara_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_atualizandoMascara || sender is not TextBox caixaTexto)
        {
            return;
        }

        var textoFormatado = FormatarTextoCampo(caixaTexto, caixaTexto.Text);

        if (caixaTexto.Text == textoFormatado)
        {
            if (caixaTexto == CaixaTextoDataNascimento)
            {
                AtualizarSituacaoPeloNascimento();
            }

            return;
        }

        _atualizandoMascara = true;
        caixaTexto.Text = textoFormatado;
        caixaTexto.CaretIndex = caixaTexto.Text.Length;
        _atualizandoMascara = false;

        if (caixaTexto == CaixaTextoDataNascimento)
        {
            AtualizarSituacaoPeloNascimento();
        }
    }

    private string FormatarTextoCampo(TextBox caixaTexto, string valor)
    {
        if (caixaTexto == CaixaTextoCPF)
        {
            return FormatarCpf(valor);
        }

        if (caixaTexto == CaixaTextoRA)
        {
            return FormatarRa(valor);
        }

        if (caixaTexto == CaixaTextoDataNascimento)
        {
            return FormatarData(valor);
        }

        if (caixaTexto == CaixaTextoCEP)
        {
            return FormatarCep(valor);
        }

        if (caixaTexto == CaixaTextoTelefone)
        {
            return FormatarTelefone(valor);
        }

        if (caixaTexto == CaixaTextoQuantidadeFilhos)
        {
            return ObterApenasDigitos(valor, 2);
        }

        if (caixaTexto == CaixaTextoAnoEscolar)
        {
            return ObterApenasDigitos(valor, 4);
        }

        if (caixaTexto == CaixaTextoPeso)
        {
            return FormatarPeso(valor);
        }

        if (caixaTexto == CaixaTextoAltura)
        {
            return FormatarAltura(valor);
        }

        if (caixaTexto == CaixaTextoTamanhoCabeca)
        {
            return FormatarTamanhoCabeca(valor);
        }

        if (caixaTexto == CaixaTextoTamanhoCalcado)
        {
            return ObterApenasDigitos(valor, 2);
        }

        if (caixaTexto == CaixaTextoCategoriaCNH)
        {
            return FormatarCategoriaCnh(valor);
        }

        if (caixaTexto == CaixaTextoEmail)
        {
            return valor.Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }

        if (CampoAceitaSomenteLetras(caixaTexto))
        {
            return ObterApenasLetras(valor);
        }

        return valor;
    }

    private bool CampoAceitaSomenteLetras(TextBox caixaTexto)
    {
        return caixaTexto == CaixaTextoNomeConscrito ||
               caixaTexto == CaixaTextoNomeMae ||
               caixaTexto == CaixaTextoPaisResidencia ||
               caixaTexto == CaixaTextoMunicipioResidencia;
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

    private static string FormatarData(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 8);

        return digitos.Length switch
        {
            > 4 => $"{digitos[..2]}/{digitos[2..4]}/{digitos[4..]}",
            > 2 => $"{digitos[..2]}/{digitos[2..]}",
            _ => digitos
        };
    }

    private static string FormatarCep(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 8);
        return digitos.Length > 5 ? $"{digitos[..5]}-{digitos[5..]}" : digitos;
    }

    private static string FormatarTelefone(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 11);

        return digitos.Length switch
        {
            > 10 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
            > 6 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
            > 2 => $"({digitos[..2]}) {digitos[2..]}",
            _ => digitos
        };
    }

    private static string ObterApenasDigitos(string valor, int limite)
    {
        return string.Concat(valor.Where(char.IsDigit).Take(limite));
    }

    private static string FormatarPeso(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 3);
        return string.IsNullOrWhiteSpace(digitos) ? string.Empty : $"{digitos} kg";
    }

    private static string FormatarAltura(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 3);

        return digitos.Length switch
        {
            3 => $"{digitos[..1]},{digitos[1..]}m",
            2 => $"{digitos[..1]},{digitos[1..]}",
            _ => digitos
        };
    }

    private static string FormatarTamanhoCabeca(string valor)
    {
        var digitos = ObterApenasDigitos(valor, 3);
        return string.IsNullOrWhiteSpace(digitos) ? string.Empty : $"{digitos}cm";
    }

    private static string ObterApenasLetras(string valor)
    {
        return string.Concat(valor.Where(caractere =>
            char.IsLetter(caractere) ||
            char.IsWhiteSpace(caractere) ||
            caractere == '-' ||
            caractere == '\''));
    }

    private static string FormatarCategoriaCnh(string valor)
    {
        var letras = string.Concat(valor
            .ToUpperInvariant()
            .Where(caractere => caractere is >= 'A' and <= 'E')
            .Take(2));

        return letras.Length == 2 && letras[0] != 'A'
            ? letras[..1]
            : letras;
    }

    private bool ValidarInformacoesBasicas(Conscrito conscrito)
    {
        if (string.IsNullOrWhiteSpace(conscrito.Nome) ||
            string.IsNullOrWhiteSpace(conscrito.CPF) ||
            string.IsNullOrWhiteSpace(conscrito.RA) ||
            string.IsNullOrWhiteSpace(conscrito.NomeMae) ||
            string.IsNullOrWhiteSpace(conscrito.DataNascimento) ||
            string.IsNullOrWhiteSpace(conscrito.PaisResidencia) ||
            string.IsNullOrWhiteSpace(conscrito.MunicipioResidencia) ||
            string.IsNullOrWhiteSpace(conscrito.ZonaResidencia) ||
            string.IsNullOrWhiteSpace(conscrito.Peso) ||
            string.IsNullOrWhiteSpace(conscrito.Altura) ||
            string.IsNullOrWhiteSpace(conscrito.TamanhoCabeca) ||
            string.IsNullOrWhiteSpace(conscrito.TamanhoCalcado))
        {
            TextoFeedbackCadastroConscrito.Text = "Preencha os campos obrigatórios das informações básicas para seleção.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        if (!RaValido(conscrito.RA))
        {
            TextoFeedbackCadastroConscrito.Text = "O RA deve conter exatamente 12 dígitos.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        if (!CpfValido(conscrito.CPF))
        {
            TextoFeedbackCadastroConscrito.Text = "O CPF deve conter 11 dígitos.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        if (!DataNascimentoValida(conscrito.DataNascimento, out var dataNascimento))
        {
            TextoFeedbackCadastroConscrito.Text = "A data de nascimento deve estar no formato dd/mm/aaaa.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        var anoLimite = ObterAnoLimiteAlistamento();
        if (dataNascimento.Year > anoLimite)
        {
            TextoFeedbackCadastroConscrito.Text = $"Só é permitido cadastrar conscritos nascidos até {anoLimite}.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        if (!MedidasBasicasValidas(conscrito))
        {
            TextoFeedbackCadastroConscrito.Text = "Confira peso, altura, tamanho da cabeça e calçado nas informações básicas.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        return true;
    }

    private static bool RaValido(string valor)
    {
        return ObterApenasDigitos(valor, 20).Length == 12;
    }

    private static bool CpfValido(string valor)
    {
        return ObterApenasDigitos(valor, 20).Length == 11;
    }

    private static bool DataNascimentoValida(string valor, out DateTime dataNascimento)
    {
        return DateTime.TryParseExact(
            valor.Trim(),
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dataNascimento);
    }

    private static bool MedidasBasicasValidas(Conscrito conscrito)
    {
        return ObterApenasDigitos(conscrito.Peso, 10).Length is >= 2 and <= 3 &&
               ObterApenasDigitos(conscrito.Altura, 10).Length == 3 &&
               ObterApenasDigitos(conscrito.TamanhoCabeca, 10).Length is >= 2 and <= 3 &&
               ObterApenasDigitos(conscrito.TamanhoCalcado, 10).Length == 2;
    }

    private static bool AnoValido(string valor)
    {
        return ObterApenasDigitos(valor, 10).Length == 4;
    }

    private static bool EmailValido(string valor)
    {
        var email = valor.Trim();
        var indiceArroba = email.IndexOf('@');
        return indiceArroba > 0 && indiceArroba == email.LastIndexOf('@') && indiceArroba < email.Length - 1;
    }

    private static bool CategoriaCnhValida(string valor)
    {
        var categoria = valor.Trim().ToUpperInvariant();
        return categoria is "A" or "B" or "C" or "D" or "E" or "AB" or "AC" or "AD" or "AE";
    }

    /// <summary>
    /// Valida a etapa atual antes de permitir avancar no wizard.
    /// </summary>
    private bool ValidarEtapaAtualFormulario()
    {
        var areaValidacao = ObterAreaValidacaoEtapaAtual();
        if (FormularioEstaPreenchido(areaValidacao))
        {
            return true;
        }

        TextoFeedbackCadastroConscrito.Text = "Preencha todos os campos desta etapa antes de avançar.";
        return false;
    }

    /// <summary>
    /// Valida todos os campos antes de salvar a ficha final.
    /// </summary>
    private bool ValidarFormularioCompleto()
    {
        if (FormularioEstaPreenchido(PainelFormularioCadastro))
        {
            return true;
        }

        TextoFeedbackCadastroConscrito.Text = "Preencha todos os campos do formulário antes de salvar.";
        return false;
    }

    private DependencyObject ObterAreaValidacaoEtapaAtual()
    {
        return _etapaWizardAtual switch
        {
            EtapaWizardInformacoesBasicas => SecaoInformacoesBasicas,
            EtapaWizardBlocoA => SecaoBlocoA,
            EtapaWizardBlocoB => SecaoBlocoB,
            EtapaWizardBlocoC => SecaoBlocoC,
            EtapaWizardBlocoD => SecaoBlocoD,
            EtapaWizardBlocoE => SecaoBlocoE,
            EtapaWizardBlocoG => SecaoBlocoG,
            EtapaWizardBlocoH => SecaoBlocoH,
            EtapaWizardBlocoI => SecaoBlocoI,
            EtapaWizardBlocoJ => SecaoBlocoJ,
            EtapaWizardManifestacao => SecaoManifestacaoDesejoServir,
            _ => PainelFormularioCadastro
        };
    }

    /// <summary>
    /// Percorre os controles visiveis e verifica se TextBox e ComboBox estao preenchidos.
    /// </summary>
    private static bool FormularioEstaPreenchido(DependencyObject elemento)
    {
        if (elemento is UIElement { Visibility: Visibility.Collapsed })
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

    /// <summary>
    /// Monta o objeto Conscrito usando os valores digitados nos controles da tela.
    /// </summary>
    private Conscrito MontarConscritoPeloFormulario()
    {
        VidaPessoal vidaPessoal = new VidaPessoal()
        {
            Endereco = CaixaTextoEndereco.Text.Trim(),
            Bairro = CaixaTextoBairro.Text.Trim(),
            CEP = CaixaTextoCEP.Text.Trim(),
            Telefone = CaixaTextoTelefone.Text.Trim(),
            Email = CaixaTextoEmail.Text.Trim(),
            Ocupacao = CaixaTextoOcupacao.Text.Trim(),
            MoraCom = CaixaTextoMoraCom.Text.Trim(),
            EstadoCivil = ObterTextoSelecionado(ComboEstadoCivil),
            PossuiFilhos = ObterTextoSelecionado(ComboPossuiFilhos),
            QuantidadeFilhos = CaixaTextoQuantidadeFilhos.Text.Trim(),
            QuemTrabalhaNaFamilia = CaixaTextoQuemTrabalhaFamilia.Text.Trim(),
            QuemSustentaAFamilia = CaixaTextoQuemSustentaFamilia.Text.Trim(),
            RecebeAuxilioGovernamental = ObterTextoSelecionado(ComboRecebeAuxilioGovernamental),
            QualAuxilioGovernamental = CaixaTextoQualAuxilioGovernamental.Text.Trim()
        };

        Arrimo arrimo = new Arrimo()
        {
            SituacaoArrimo = ObterTextoSelecionado(ComboSituacaoArrimo),
            EstudaAtualmente = ObterTextoSelecionado(ComboEstudaAtualmente),
            AnoQueEstaCursandoOuUltimoAnoQueCursou = CaixaTextoAnoEscolar.Text.Trim()
        };

        Cursos cursos = new Cursos()
        {
            TemCursosProfissionalizantes = ObterTextoSelecionado(ComboTemCursosProfissionalizantes),
            QuaisCursosProfissionalizantes = CaixaTextoQuaisCursos.Text.Trim(),
            ComprovaCursosProfissionalizantes = ObterTextoSelecionado(ComboComprovaCursos)
        };

        Experiencia experiencia = new Experiencia()
        {
            ExperienciaProfissional = ObterTextoSelecionado(ComboExperienciaProfissional),
            QuaisExperienciasProfissionais = CaixaTextoQuaisExperiencias.Text.Trim(),
            ComprovaExperienciaProfissional = ObterTextoSelecionado(ComboComprovaExperiencia)
        };

        Habilitacao habilitacao = new Habilitacao()
        {
            PossuiCNH = ObterTextoSelecionado(ComboPossuiCNH),
            RealizandoCursoParaHabilitacao = RespostaEhSim(ObterTextoSelecionado(ComboPossuiCNH))
                ? "Não"
                : ObterTextoSelecionado(ComboCursoHabilitacao),
            CategoriaCNH = CaixaTextoCategoriaCNH.Text.Trim()
        };


        Esportes esportes = new Esportes()
        {
            PraticaEsportes = ObterTextoSelecionado(ComboPraticaEsportes),
            QuaisEsportes = CaixaTextoQuaisEsportes.Text.Trim(),
            EhOuJaFoiFederado = ObterTextoSelecionado(ComboFederado),
            SabeNadar = ObterTextoSelecionado(ComboSabeNadar)
        };

        Lazer lazer = new Lazer()
        {
            OQueFazNasHorasDeLazer = CaixaTextoLazer.Text.Trim()
        };

        Saude saude = new Saude()
        {
            JaTeveProblemaSaude = ObterTextoSelecionado(ComboProblemaSaude),
            QualProblemaSaude = CaixaTextoQualProblemaSaude.Text.Trim(),
            UsaRemedioControlado = ObterTextoSelecionado(ComboRemedioControlado),
            QualRemedioControlado = CaixaTextoQualRemedioControlado.Text.Trim(),
            ParaQueUsaRemedioControlado = CaixaTextoParaQueRemedio.Text.Trim(),
            HaQuantoTempoUsaRemedioControlado = CaixaTextoHaQuantoTempoRemedio.Text.Trim(),
            PorQuantoTempoAindaUsaraRemedio = CaixaTextoTempoRestanteRemedio.Text.Trim(),
            JaEsteveInternadoHospitalOuClinicaPsiquiatrica = ObterTextoSelecionado(ComboInternacaoPsiquiatrica),
            MotivoInternacao = CaixaTextoMotivoInternacao.Text.Trim(),
            TempoInternacao = CaixaTextoTempoInternacao.Text.Trim(),
            Fuma = ObterTextoSelecionado(ComboFuma),
            HaQuantoTempoFuma = CaixaTextoTempoFuma.Text.Trim(),
            FazUsoBebidaAlcoolica = ObterTextoSelecionado(ComboBebidaAlcoolica),
            FrequenciaBebidaAlcoolica = CaixaTextoFrequenciaBebida.Text.Trim(),
            JaExperimentouDrogas = ObterTextoSelecionado(ComboExperimentouDrogas),
            QualDroga = CaixaTextoQualDroga.Text.Trim(),
            AindaFazUsoDroga = ObterTextoSelecionado(ComboAindaUsaDroga),
            FrequenciaUsoDroga = CaixaTextoFrequenciaDroga.Text.Trim(),
            QuandoFoiUltimaVezQueUtilizouDroga = CaixaTextoUltimaVezDroga.Text.Trim(),
            PossuiParenteUsuarioDrogas = ObterTextoSelecionado(ComboParenteUsuarioDrogas),
            QuemParenteUsuarioDrogas = CaixaTextoQuemParenteUsuarioDrogas.Text.Trim(),
            ComoParenteUsuarioDrogasAfetaSuaVida = CaixaTextoImpactoParenteUsuarioDrogas.Text.Trim(),
            PossuiParenteComHistoricoTranstornoPsiquiatrico = ObterTextoSelecionado(ComboParenteTranstornoPsiquiatrico),
            QuemParenteComHistoricoTranstornoPsiquiatrico = CaixaTextoQuemParenteTranstornoPsiquiatrico.Text.Trim(),
            ComoTranstornoPsiquiatricoAfetaSuaVida = CaixaTextoImpactoTranstornoPsiquiatrico.Text.Trim()
        };

        Infracao infracao = new Infracao()
        {
            JaFoiDetidoPelaPolicia = ObterTextoSelecionado(ComboDetidoPelaPolicia),
            QualFoiAInfracao = CaixaTextoQualInfracao.Text.Trim(),
            OutrosAtosInfracionais = CaixaTextoOutrosAtosInfracionais.Text.Trim()
        };

        var dataNascimento = CaixaTextoDataNascimento.Text.Trim();
        var situacao = ObterSituacaoPeloNascimento(dataNascimento, ObterTextoSelecionado(ComboSituacaoConscrito));

        var conscrito = new Conscrito
        {
            Id = _idConscritoEmEdicao ?? string.Empty,
            Nome = CaixaTextoNomeConscrito.Text.Trim(),
            CPF = CaixaTextoCPF.Text.Trim(),
            RA = CaixaTextoRA.Text.Trim(),
            Situacao = situacao,
            NomeMae = CaixaTextoNomeMae.Text.Trim(),
            DataNascimento = dataNascimento,
            PaisResidencia = CaixaTextoPaisResidencia.Text.Trim(),
            MunicipioResidencia = CaixaTextoMunicipioResidencia.Text.Trim(),
            ZonaResidencia = ObterTextoSelecionado(ComboZonaResidencia),
            Peso = CaixaTextoPeso.Text.Trim(),
            Altura = CaixaTextoAltura.Text.Trim(),
            TamanhoCabeca = CaixaTextoTamanhoCabeca.Text.Trim(),
            TamanhoCalcado = CaixaTextoTamanhoCalcado.Text.Trim(),
            DesejaServir = ObterTextoSelecionado(ComboDesejaServir),
            Observacao = CaixaTextoObservacaoManifestacao.Text.Trim(),
            Entrevista_Vida_Pessoal = vidaPessoal,
            Entrevista_Arrimo_De_Familia = arrimo,
            Entrevista_Cursos = cursos,
            Entrevista_Experiencia = experiencia,
            Entrevista_Habilitacao = habilitacao,
            Entrevista_Esportes = esportes,
            Entrevista_Lazer = lazer,
            Entrevista_Saude = saude,
            Entrevista_Infracao = infracao
        };

        PreservarAndamentoExistente(conscrito);
        return conscrito;
    }

    private static string ObterTextoSelecionado(ComboBox comboBox)
    {
        var texto = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;
        return texto == "Selecione" ? string.Empty : texto;
    }

    private static string ObterSituacaoPeloNascimento(string dataNascimento, string situacaoSelecionada)
    {
        var anoLimite = ObterAnoLimiteAlistamento();
        return DataNascimentoValida(dataNascimento, out var data) && data.Year < anoLimite
            ? "Refratário"
            : situacaoSelecionada;
    }

    private void PreservarAndamentoExistente(Conscrito conscrito)
    {
        var existente = ObterConscritoEmEdicao();
        if (existente is null)
        {
            return;
        }

        conscrito.PrimeiraEtapaConcluida = existente.PrimeiraEtapaConcluida;
        conscrito.SegundaEtapaConcluida = existente.SegundaEtapaConcluida;
        conscrito.TerceiraEtapaConcluida = existente.TerceiraEtapaConcluida;
        conscrito.QuartaEtapaConcluida = existente.QuartaEtapaConcluida;
        conscrito.Faltoso = existente.Faltoso;
        conscrito.Entrevista_Medica = existente.Entrevista_Medica;
    }

    private void AplicarAndamentoDaEtapa(Conscrito conscrito)
    {
        if (_modoEntrevistaTecnica)
        {
            conscrito.TerceiraEtapaConcluida = true;
        }
        else
        {
            conscrito.PrimeiraEtapaConcluida = true;
        }

        conscrito.Faltoso = false;
    }

    private void AtualizarSituacaoPeloNascimento()
    {
        var anoLimite = ObterAnoLimiteAlistamento();
        if (DataNascimentoValida(CaixaTextoDataNascimento.Text, out var data) &&
            data.Year < anoLimite)
        {
            SelecionarComboPorTexto(ComboSituacaoConscrito, "Refratário");
        }
    }

    /// <summary>
    /// Aplica pesquisa e filtros rapidos na lista de conscritos.
    /// </summary>
    private void AplicarFiltrosLista()
    {
        IEnumerable<Conscrito> consulta = _conscritosCarregados;
        var pesquisa = CaixaPesquisaConscrito.Text.Trim();

        if (!string.IsNullOrWhiteSpace(pesquisa))
        {
            consulta = consulta.Where(conscrito =>
                ContemTexto(conscrito.Nome, pesquisa) ||
                ContemTexto(conscrito.CPF, pesquisa) ||
                ContemTexto(conscrito.RA, pesquisa));
        }

        var situacoesSelecionadas = ObterSituacoesSelecionadas();
        if (situacoesSelecionadas.Count > 0)
        {
            consulta = consulta.Where(conscrito => situacoesSelecionadas.Contains(NormalizarSituacao(conscrito.Situacao)));
        }

        var andamentosSelecionados = ObterAndamentosSelecionados();
        if (andamentosSelecionados.Count > 0)
        {
            consulta = consulta.Where(conscrito => andamentosSelecionados.Contains(conscrito.AndamentoProcesso));
        }

        if (FiltroTrabalha.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => TemTrabalhoDeclarado(conscrito.Entrevista_Vida_Pessoal.Ocupacao));
        }

        if (FiltroRecebeAuxilio.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Vida_Pessoal.RecebeAuxilioGovernamental));
        }

        if (FiltroEstuda.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Arrimo_De_Familia.EstudaAtualmente));
        }

        if (FiltroExperiencia.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Experiencia.ExperienciaProfissional));
        }

        if (FiltroProblemaSaude.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.Entrevista_Saude.JaTeveProblemaSaude));
        }

        var filtrosDesejo = new List<string>();
        if (FiltroDesejaServirSim.IsChecked == true)
        {
            filtrosDesejo.Add("Sim");
        }

        if (FiltroDesejaServirNao.IsChecked == true)
        {
            filtrosDesejo.Add("Não");
        }

        if (filtrosDesejo.Count > 0)
        {
            consulta = consulta.Where(conscrito => filtrosDesejo.Contains(NormalizarResposta(conscrito.DesejaServir)));
        }

        var listaFiltrada = consulta.ToList();
        GradeConscritos.ItemsSource = listaFiltrada;
        TextoResumoLista.Text = $"{listaFiltrada.Count} conscritos encontrados.";
    }

    private HashSet<string> ObterSituacoesSelecionadas()
    {
        var situacoes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FiltroSituacaoTG.IsChecked == true) situacoes.Add("TG");
        if (FiltroSituacaoSubstituto.IsChecked == true) situacoes.Add("Substituto");
        if (FiltroSituacaoApto.IsChecked == true) situacoes.Add("Apto");
        if (FiltroSituacaoInapto.IsChecked == true) situacoes.Add("Inapto");
        if (FiltroSituacaoDispensado.IsChecked == true) situacoes.Add("Dispensado");
        if (FiltroSituacaoRefratario.IsChecked == true) situacoes.Add("Refratário");
        if (FiltroSituacaoIndefinido.IsChecked == true) situacoes.Add("Indefinido");

        return situacoes;
    }

    private HashSet<string> ObterAndamentosSelecionados()
    {
        var andamentos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (FiltroAndamentoEmAndamento.IsChecked == true) andamentos.Add("Em andamento");
        if (FiltroAndamentoFinalizado.IsChecked == true) andamentos.Add("Finalizado");
        if (FiltroAndamentoFaltoso.IsChecked == true) andamentos.Add("Faltoso");

        return andamentos;
    }

    private static bool ContemTexto(string? valor, string pesquisa)
    {
        return !string.IsNullOrWhiteSpace(valor) &&
               valor.Contains(pesquisa, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RespostaEhSim(string? valor)
    {
        return string.Equals(NormalizarResposta(valor), "Sim", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizarResposta(string? valor)
    {
        return valor?.Trim().Replace("Não", "Nao", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
    }

    private static string NormalizarSituacao(string? situacao)
    {
        return string.IsNullOrWhiteSpace(situacao) ? "Indefinido" : situacao.Trim();
    }

    private static bool TemTrabalhoDeclarado(string? ocupacao)
    {
        if (string.IsNullOrWhiteSpace(ocupacao))
        {
            return false;
        }

        var ocupacaoNormalizada = ocupacao.Trim();
        return !string.Equals(ocupacaoNormalizada, "Nao", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Não", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Nao trabalha", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Não trabalha", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(ocupacaoNormalizada, "Desempregado", StringComparison.OrdinalIgnoreCase);
    }

    private static void SelecionarComboPorTexto(ComboBox comboBox, string valor, string? valorPadrao = null)
    {
        if (string.IsNullOrWhiteSpace(valor) && !string.IsNullOrWhiteSpace(valorPadrao))
        {
            valor = valorPadrao;
        }

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

    private void LimparCamposFormulario()
    {
        LimparControles(PainelFormularioCadastro);
        AtualizarCamposCondicionais();
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

        var quantidadeFilhos = VisualTreeHelper.GetChildrenCount(elemento);
        for (var indice = 0; indice < quantidadeFilhos; indice++)
        {
            LimparControles(VisualTreeHelper.GetChild(elemento, indice));

        }
    }

    private bool ValidarEtapaAtual(Conscrito c)
    {
        switch (_etapaWizardAtual)
        {
            case EtapaWizardInformacoesBasicas:
                return ValidarInformacoesBasicas(c);

            case EtapaWizardBlocoA:
                return ValidarBlocoA(c);

            case EtapaWizardBlocoB:
                return ValidarBlocoB(c);

            case EtapaWizardBlocoC:
                return ValidarBlocoC(c);

            case EtapaWizardBlocoD:
                return ValidarBlocoD(c);

            case EtapaWizardBlocoE:
                return ValidarBlocoE(c);

            case EtapaWizardBlocoG:
                return ValidarBlocoG(c);

            case EtapaWizardBlocoH:
                return ValidarBlocoH(c);

            case EtapaWizardBlocoI:
                return ValidarBlocoI(c);

            case EtapaWizardBlocoJ:
                return ValidarBlocoJ(c);

            case EtapaWizardManifestacao:
                return !string.IsNullOrWhiteSpace(c.Situacao) &&
                       !string.IsNullOrWhiteSpace(c.DesejaServir) &&
                       !string.IsNullOrWhiteSpace(c.Observacao);

            default:
                return true;
        }
    }


    private bool ValidarBlocoA(Conscrito c)
    {
        var vp = c.Entrevista_Vida_Pessoal;

        return !string.IsNullOrWhiteSpace(vp.Endereco) &&
               !string.IsNullOrWhiteSpace(vp.Bairro) &&
               !string.IsNullOrWhiteSpace(vp.CEP) &&
               !string.IsNullOrWhiteSpace(vp.Telefone) &&
               EmailValido(vp.Email) &&
               !string.IsNullOrWhiteSpace(vp.Ocupacao) &&
               !string.IsNullOrWhiteSpace(vp.MoraCom) &&
               !string.IsNullOrWhiteSpace(vp.EstadoCivil) &&
               !string.IsNullOrWhiteSpace(vp.PossuiFilhos) &&
               RespostaCondicionalPreenchida(vp.PossuiFilhos, vp.QuantidadeFilhos) &&
               !string.IsNullOrWhiteSpace(vp.QuemTrabalhaNaFamilia) &&
               !string.IsNullOrWhiteSpace(vp.QuemSustentaAFamilia) &&
               RespostaCondicionalPreenchida(vp.RecebeAuxilioGovernamental, vp.QualAuxilioGovernamental);
    }

    private bool ValidarBlocoB(Conscrito c)
    {
        var a = c.Entrevista_Arrimo_De_Familia;

        return !string.IsNullOrWhiteSpace(a.SituacaoArrimo) &&
               !string.IsNullOrWhiteSpace(a.EstudaAtualmente) &&
               AnoValido(a.AnoQueEstaCursandoOuUltimoAnoQueCursou);
    }

    private bool ValidarBlocoC(Conscrito c)
    {
        var cursos = c.Entrevista_Cursos;

        return RespostaCondicionalPreenchida(
            cursos.TemCursosProfissionalizantes,
            cursos.QuaisCursosProfissionalizantes,
            cursos.ComprovaCursosProfissionalizantes);
    }

    private bool ValidarBlocoD(Conscrito c)
    {
        var exp = c.Entrevista_Experiencia;

        return RespostaCondicionalPreenchida(
            exp.ExperienciaProfissional,
            exp.QuaisExperienciasProfissionais,
            exp.ComprovaExperienciaProfissional);
    }

    private bool ValidarBlocoE(Conscrito c)
    {
        var h = c.Entrevista_Habilitacao;

        if (string.IsNullOrWhiteSpace(h.PossuiCNH))
        {
            return false;
        }

        if (RespostaEhSim(h.PossuiCNH))
        {
            return !string.IsNullOrWhiteSpace(h.CategoriaCNH) &&
                   CategoriaCnhValida(h.CategoriaCNH);
        }

        return !string.IsNullOrWhiteSpace(h.RealizandoCursoParaHabilitacao);
    }

    private bool ValidarBlocoG(Conscrito c)
    {
        var e = c.Entrevista_Esportes;

        return RespostaCondicionalPreenchida(e.PraticaEsportes, e.QuaisEsportes, e.EhOuJaFoiFederado) &&
               !string.IsNullOrWhiteSpace(e.SabeNadar);
    }

    private bool ValidarBlocoH(Conscrito c)
    {
        return !string.IsNullOrWhiteSpace(c.Entrevista_Lazer.OQueFazNasHorasDeLazer);
    }

    private bool ValidarBlocoI(Conscrito c)
    {
        var s = c.Entrevista_Saude;

        return RespostaCondicionalPreenchida(s.JaTeveProblemaSaude, s.QualProblemaSaude) &&
               RespostaCondicionalPreenchida(
                   s.UsaRemedioControlado,
                   s.QualRemedioControlado,
                   s.ParaQueUsaRemedioControlado,
                   s.HaQuantoTempoUsaRemedioControlado,
                   s.PorQuantoTempoAindaUsaraRemedio) &&
               RespostaCondicionalPreenchida(s.JaEsteveInternadoHospitalOuClinicaPsiquiatrica, s.MotivoInternacao, s.TempoInternacao) &&
               RespostaCondicionalPreenchida(s.Fuma, s.HaQuantoTempoFuma) &&
               RespostaCondicionalPreenchida(s.FazUsoBebidaAlcoolica, s.FrequenciaBebidaAlcoolica) &&
               RespostaCondicionalPreenchida(s.JaExperimentouDrogas, s.QualDroga, s.AindaFazUsoDroga, s.QuandoFoiUltimaVezQueUtilizouDroga) &&
               (!RespostaEhSim(s.JaExperimentouDrogas) || RespostaCondicionalPreenchida(s.AindaFazUsoDroga, s.FrequenciaUsoDroga)) &&
               RespostaCondicionalPreenchida(
                   s.PossuiParenteUsuarioDrogas,
                   s.QuemParenteUsuarioDrogas,
                   s.ComoParenteUsuarioDrogasAfetaSuaVida) &&
               RespostaCondicionalPreenchida(
                   s.PossuiParenteComHistoricoTranstornoPsiquiatrico,
                   s.QuemParenteComHistoricoTranstornoPsiquiatrico,
                   s.ComoTranstornoPsiquiatricoAfetaSuaVida);
    }

    private bool ValidarBlocoJ(Conscrito c)
    {
        var i = c.Entrevista_Infracao;

        return RespostaCondicionalPreenchida(
            i.JaFoiDetidoPelaPolicia,
            i.QualFoiAInfracao,
            i.OutrosAtosInfracionais);
    }

    private static bool RespostaCondicionalPreenchida(string resposta, params string[] detalhesObrigatorios)
    {
        if (string.IsNullOrWhiteSpace(resposta))
        {
            return false;
        }

        if (!RespostaEhSim(resposta))
        {
            return true;
        }

        foreach (var detalhe in detalhesObrigatorios)
        {
            if (string.IsNullOrWhiteSpace(detalhe))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidarFichaCompleta(Conscrito c)
    {
        return ValidarInformacoesBasicas(c) &&
               ValidarBlocoA(c) &&
               ValidarBlocoB(c) &&
               ValidarBlocoC(c) &&
               ValidarBlocoD(c) &&
               ValidarBlocoE(c) &&
               ValidarBlocoG(c) &&
               ValidarBlocoH(c) &&
               ValidarBlocoI(c) &&
               ValidarBlocoJ(c) &&
               !string.IsNullOrWhiteSpace(c.Situacao) &&
               !string.IsNullOrWhiteSpace(c.DesejaServir) &&
               !string.IsNullOrWhiteSpace(c.Observacao);
    }

}
