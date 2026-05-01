using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaPrimeiraEtapa : Window
{
    private const int EtapaWizardInformacoesBasicas = 0;
    private const int EtapaWizardBlocoA = 1;
    private const int EtapaWizardBlocoB = 2;
    private const int EtapaWizardBlocoC = 3;
    private const int EtapaWizardBlocoD = 4;
    private const int EtapaWizardBlocoE = 5;
    private const int EtapaWizardBlocoF = 6;
    private const int EtapaWizardBlocoG = 7;
    private const int EtapaWizardBlocoH = 8;
    private const int EtapaWizardBlocoI = 9;
    private const int EtapaWizardBlocoJ = 10;
    private const int EtapaWizardManifestacao = 11;
    private const int EtapaWizardConfirmacao = 12;
    private const int TotalEtapasWizard = 13;

    private static readonly Brush FundoEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F5EA")!);
    private static readonly Brush FundoEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2FBF6")!);
    private static readonly Brush FundoEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")!);
    private static readonly Brush BordaEtapaAtiva = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#196743")!);
    private static readonly Brush BordaEtapaConcluida = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8CC7A3")!);
    private static readonly Brush BordaEtapaInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9E1DD")!);

    private readonly Conscrito? _conscritoInicial;
    private readonly bool _abrirListaAoIniciar;
    private string? _idConscritoEmEdicao;
    private int _etapaWizardAtual = EtapaWizardInformacoesBasicas;
    private bool _atualizandoMascara;
    private List<Conscrito> _conscritosCarregados = [];

    public TelaPrimeiraEtapa(Conscrito? conscrito = null, bool abrirListaAoIniciar = false)
    {
        _conscritoInicial = conscrito;
        _abrirListaAoIniciar = abrirListaAoIniciar;
        InitializeComponent();
        CarregarConscritos();
        PrepararTela();

        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            CadastrarEntrevistador.Visibility = Visibility.Collapsed;
            CadastrarEntrevistador.IsEnabled = false;
        }
    }

    private bool EmModoEdicao => !string.IsNullOrWhiteSpace(_idConscritoEmEdicao);

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
        PrepararNovoCadastro();
        MostrarCadastroConscrito();
    }

    private void EtapaFuturaBotao_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Esta etapa ainda será desenvolvida em tela própria.", "Em desenvolvimento", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        TelaPainelControle janela = new TelaPainelControle();
        janela.Show();
        this.Close();
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarListaConscritos();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {


        var telaCadastroUsuario = new TelaCadastroUsuario
        {
            Owner = this
        };

        telaCadastroUsuario.ShowDialog();
    }

    private void SairSistemaBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoAutenticacao.EncerrarSessao();
        var telaLogin = new TelaLogin();
        telaLogin.Show();
        Close();
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
        FiltroSituacaoIndefinido.IsChecked = false;
        FiltroTrabalha.IsChecked = false;
        FiltroRecebeAuxilio.IsChecked = false;
        FiltroEstuda.IsChecked = false;
        FiltroExperiencia.IsChecked = false;
        FiltroProblemaSaude.IsChecked = false;
        FiltroDesejaServirSim.IsChecked = false;
        FiltroDesejaServirNao.IsChecked = false;

        AplicarFiltrosLista();
    }

    private void SalvarConscritoBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        var conscrito = MontarConscritoPeloFormulario();

        if (_etapaWizardAtual != EtapaWizardConfirmacao)
        {
            if (!ValidarEtapaAtualFormulario())
            {
                return;
            }

            AvancarEtapaWizard();
            return;
        }

        if (!ValidarFormularioCompleto())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(conscrito.Situacao))
        {
            conscrito.Situacao = "Indefinido";
        }

        if (EmModoEdicao)
        {
            ServicoArmazenamentoConscritos.Atualizar(conscrito);
        }
        else
        {
            ServicoArmazenamentoConscritos.Adicionar(conscrito);
        }

        TelaPainelControle janela = new TelaPainelControle();
        janela.Show();
        this.Close();
    }

    private void LimparFormularioBotao_Click(object sender, RoutedEventArgs e)
    {
        if (EmModoEdicao)
        {
            TelaPainelControle janela = new TelaPainelControle();
            janela.Show();
            this.Close();
            return;
        }

        LimparCamposFormulario();
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
    }

    private void ExcluirConscritoBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!EmModoEdicao || string.IsNullOrWhiteSpace(_idConscritoEmEdicao))
        {
            return;
        }

        var confirmacao = MessageBox.Show(
            "Deseja realmente excluir este conscrito?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmacao != MessageBoxResult.Yes)
        {
            return;
        }

        ServicoArmazenamentoConscritos.Excluir(_idConscritoEmEdicao);
        TelaPainelControle janela = new TelaPainelControle();
        janela.Show();
        this.Close();
    }

    private void CarregarConscritos()
    {
        _conscritosCarregados = ServicoArmazenamentoConscritos.ObterTodos()
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
        AtualizarWizardFormulario();
        ScrollFormularioCadastro.ScrollToHome();
    }

    private void MostrarListaConscritos()
    {
        VisaoInicial.Visibility = Visibility.Collapsed;
        VisaoCadastroConscrito.Visibility = Visibility.Collapsed;
        VisaoListaConscritos.Visibility = Visibility.Visible;
        GradeConscritos.SelectedItem = null;
        CarregarConscritos();
    }

    private void PrepararNovoCadastro()
    {
        _idConscritoEmEdicao = null;
        LimparCamposFormulario();
        ComboSituacaoConscrito.SelectedIndex = 0;
        TextoFeedbackCadastroConscrito.Text = string.Empty;
        DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
    }

    private void CarregarConscritoParaEdicao(Conscrito conscrito)
    {
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
        SelecionarComboPorTexto(ComboSituacaoConscrito, conscrito.Situacao, "Indefinido");
        CaixaTextoEndereco.Text = conscrito.Endereco;
        CaixaTextoBairro.Text = conscrito.Bairro;
        CaixaTextoCEP.Text = conscrito.CEP;
        CaixaTextoTelefone.Text = conscrito.Telefone;
        CaixaTextoMunicipio.Text = conscrito.Municipio;
        CaixaTextoEmail.Text = conscrito.Email;
        CaixaTextoOcupacao.Text = conscrito.Ocupacao;
        CaixaTextoMoraCom.Text = conscrito.MoraCom;
        SelecionarComboPorTexto(ComboEstadoCivil, conscrito.EstadoCivil);
        SelecionarComboPorTexto(ComboPossuiFilhos, conscrito.PossuiFilhos);
        CaixaTextoQuantidadeFilhos.Text = conscrito.QuantidadeFilhos;
        CaixaTextoQuemTrabalhaFamilia.Text = conscrito.QuemTrabalhaNaFamilia;
        CaixaTextoQuemSustentaFamilia.Text = conscrito.QuemSustentaAFamilia;
        SelecionarComboPorTexto(ComboRecebeAuxilioGovernamental, conscrito.RecebeAuxilioGovernamental);
        SelecionarComboPorTexto(ComboSituacaoArrimo, conscrito.SituacaoArrimo);
        SelecionarComboPorTexto(ComboEstudaAtualmente, conscrito.EstudaAtualmente);
        CaixaTextoAnoEscolar.Text = conscrito.AnoQueEstaCursandoOuUltimoAnoQueCursou;
        SelecionarComboPorTexto(ComboTemCursosProfissionalizantes, conscrito.TemCursosProfissionalizantes);
        CaixaTextoQuaisCursos.Text = conscrito.QuaisCursosProfissionalizantes;
        SelecionarComboPorTexto(ComboComprovaCursos, conscrito.ComprovaCursosProfissionalizantes);
        SelecionarComboPorTexto(ComboExperienciaProfissional, conscrito.ExperienciaProfissional);
        CaixaTextoQuaisExperiencias.Text = conscrito.QuaisExperienciasProfissionais;
        SelecionarComboPorTexto(ComboComprovaExperiencia, conscrito.ComprovaExperienciaProfissional);
        SelecionarComboPorTexto(ComboPossuiCNH, conscrito.PossuiCNH);
        SelecionarComboPorTexto(ComboCursoHabilitacao, conscrito.RealizandoCursoParaHabilitacao);
        CaixaTextoCategoriaCNH.Text = conscrito.CategoriaCNH;
        CaixaTextoPrimeiroPadraoPreQualificacao.Text = conscrito.PrimeiroPadraoPreQualificacao;
        CaixaTextoSegundoPadraoPreQualificacao.Text = conscrito.SegundoPadraoPreQualificacao;
        SelecionarComboPorTexto(ComboPraticaEsportes, conscrito.PraticaEsportes);
        CaixaTextoQuaisEsportes.Text = conscrito.QuaisEsportes;
        SelecionarComboPorTexto(ComboFederado, conscrito.EhOuJaFoiFederado);
        SelecionarComboPorTexto(ComboSabeNadar, conscrito.SabeNadar);
        CaixaTextoLazer.Text = conscrito.OQueFazNasHorasDeLazer;
        SelecionarComboPorTexto(ComboProblemaSaude, conscrito.JaTeveProblemaSaude);
        CaixaTextoQualProblemaSaude.Text = conscrito.QualProblemaSaude;
        SelecionarComboPorTexto(ComboRemedioControlado, conscrito.UsaRemedioControlado);
        CaixaTextoQualRemedioControlado.Text = conscrito.QualRemedioControlado;
        CaixaTextoParaQueRemedio.Text = conscrito.ParaQueUsaRemedioControlado;
        CaixaTextoHaQuantoTempoRemedio.Text = conscrito.HaQuantoTempoUsaRemedioControlado;
        CaixaTextoTempoRestanteRemedio.Text = conscrito.PorQuantoTempoAindaUsaraRemedio;
        SelecionarComboPorTexto(ComboInternacaoPsiquiatrica, conscrito.JaEsteveInternadoHospitalOuClinicaPsiquiatrica);
        CaixaTextoMotivoInternacao.Text = conscrito.MotivoInternacao;
        CaixaTextoTempoInternacao.Text = conscrito.TempoInternacao;
        SelecionarComboPorTexto(ComboFuma, conscrito.Fuma);
        CaixaTextoTempoFuma.Text = conscrito.HaQuantoTempoFuma;
        SelecionarComboPorTexto(ComboBebidaAlcoolica, conscrito.FazUsoBebidaAlcoolica);
        CaixaTextoFrequenciaBebida.Text = conscrito.FrequenciaBebidaAlcoolica;
        SelecionarComboPorTexto(ComboExperimentouDrogas, conscrito.JaExperimentouDrogas);
        CaixaTextoQualDroga.Text = conscrito.QualDroga;
        SelecionarComboPorTexto(ComboAindaUsaDroga, conscrito.AindaFazUsoDroga);
        CaixaTextoFrequenciaDroga.Text = conscrito.FrequenciaUsoDroga;
        CaixaTextoUltimaVezDroga.Text = conscrito.QuandoFoiUltimaVezQueUtilizouDroga;
        SelecionarComboPorTexto(ComboParenteUsuarioDrogas, conscrito.PossuiParenteUsuarioDrogas);
        CaixaTextoQuemParenteUsuarioDrogas.Text = conscrito.QuemParenteUsuarioDrogas;
        CaixaTextoImpactoParenteUsuarioDrogas.Text = conscrito.ComoParenteUsuarioDrogasAfetaSuaVida;
        SelecionarComboPorTexto(ComboParenteTranstornoPsiquiatrico, conscrito.PossuiParenteComHistoricoTranstornoPsiquiatrico);
        CaixaTextoQuemParenteTranstornoPsiquiatrico.Text = conscrito.QuemParenteComHistoricoTranstornoPsiquiatrico;
        CaixaTextoImpactoTranstornoPsiquiatrico.Text = conscrito.ComoTranstornoPsiquiatricoAfetaSuaVida;
        SelecionarComboPorTexto(ComboDetidoPelaPolicia, conscrito.JaFoiDetidoPelaPolicia);
        CaixaTextoQualInfracao.Text = conscrito.QualFoiAInfracao;
        CaixaTextoOutrosAtosInfracionais.Text = conscrito.OutrosAtosInfracionais;
        SelecionarComboPorTexto(ComboDesejaServir, conscrito.DesejaServir);

        MostrarCadastroConscrito();
        DefinirEtapaWizard(EtapaWizardConfirmacao);
    }

    private void VoltarEtapaWizardBotao_Click(object sender, RoutedEventArgs e)
    {
        if (_etapaWizardAtual == EtapaWizardInformacoesBasicas)
        {
            return;
        }

        DefinirEtapaWizard(_etapaWizardAtual - 1);
    }

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
        SecaoBlocoF.Visibility = (_etapaWizardAtual == EtapaWizardBlocoF || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
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
        BotaoExcluirConscrito.Visibility = EmModoEdicao ? Visibility.Visible : Visibility.Collapsed;
        TextoBotaoLimpar.Text = EmModoEdicao ? "Fechar" : "Limpar";
        IconeAvancarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Collapsed : Visibility.Visible;
        IconeSalvarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        switch (_etapaWizardAtual)
        {
            case EtapaWizardInformacoesBasicas:
                TextoTituloFormulario.Text = EmModoEdicao ? "Editar Conscrito" : "Primeira Etapa de Seleção";
                TextoDescricaoFormulario.Text = "Comece pelas informações básicas obrigatórias. Depois você seguirá bloco por bloco até chegar na revisão final.";
                TextoEtapaWizard.Text = $"Etapa 1 de {TotalEtapasWizard} · Informações básicas";
                TextoResumoEtapaWizard.Text = "Preencha os dados essenciais do conscrito para iniciar o cadastro.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case >= EtapaWizardBlocoA and <= EtapaWizardBlocoJ:
                var nomeBloco = ObterNomeBlocoWizard(_etapaWizardAtual);
                TextoTituloFormulario.Text = EmModoEdicao ? "Editar Conscrito" : "Primeira Etapa de Seleção";
                TextoDescricaoFormulario.Text = "Preencha este bloco e avance pela seta para continuar o formulário em partes menores.";
                TextoEtapaWizard.Text = $"Etapa {_etapaWizardAtual + 1} de {TotalEtapasWizard} · {nomeBloco}";
                TextoResumoEtapaWizard.Text = "Ao avançar, o próximo bloco será exibido separadamente. No final, tudo aparecerá junto para conferência.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case EtapaWizardManifestacao:
                TextoTituloFormulario.Text = EmModoEdicao ? "Editar Conscrito" : "Primeira Etapa de Seleção";
                TextoDescricaoFormulario.Text = "Preencha o bloco K e depois avance para revisar o formulário completo antes de salvar.";
                TextoEtapaWizard.Text = $"Etapa 12 de {TotalEtapasWizard} · Bloco K";
                TextoResumoEtapaWizard.Text = "Registre a manifestação do desejo de servir para concluir o preenchimento.";
                TextoBotaoSalvar.Text = "Ir para confirmação";
                break;

            default:
                TextoTituloFormulario.Text = EmModoEdicao ? "Detalhes do Conscrito" : "Confirmar Dados do Conscrito";
                TextoDescricaoFormulario.Text = "Confira o formulário completo abaixo. Se precisar, altere qualquer campo antes de salvar a ficha.";
                TextoEtapaWizard.Text = $"Etapa 13 de {TotalEtapasWizard} · Confirmação final";
                TextoResumoEtapaWizard.Text = "Toda a ficha aparece completa para revisão e ajustes finais antes do salvamento.";
                TextoBotaoSalvar.Text = EmModoEdicao ? "Salvar Alterações" : "Salvar Ficha";
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
            EtapaWizardBlocoF => "Bloco F · Pré-qualificação",
            EtapaWizardBlocoG => "Bloco G · Prática de esportes",
            EtapaWizardBlocoH => "Bloco H · Lazer",
            EtapaWizardBlocoI => "Bloco I · Saúde",
            EtapaWizardBlocoJ => "Bloco J · Ato infracional",
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

    private void CampoComMascara_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_atualizandoMascara || sender is not TextBox caixaTexto)
        {
            return;
        }

        var textoFormatado = caixaTexto == CaixaTextoCPF
            ? FormatarCpf(caixaTexto.Text)
            : caixaTexto == CaixaTextoDataNascimento
                ? FormatarData(caixaTexto.Text)
                : caixaTexto == CaixaTextoCEP
                    ? FormatarCep(caixaTexto.Text)
                    : caixaTexto == CaixaTextoTelefone
                        ? FormatarTelefone(caixaTexto.Text)
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

    private bool ValidarInformacoesBasicas(Conscrito conscrito)
    {
        if (string.IsNullOrWhiteSpace(conscrito.Nome) ||
            string.IsNullOrWhiteSpace(conscrito.CPF) ||
            string.IsNullOrWhiteSpace(conscrito.RA) ||
            string.IsNullOrWhiteSpace(conscrito.NomeMae) ||
            string.IsNullOrWhiteSpace(conscrito.DataNascimento) ||
            string.IsNullOrWhiteSpace(conscrito.PaisResidencia) ||
            string.IsNullOrWhiteSpace(conscrito.MunicipioResidencia) ||
            string.IsNullOrWhiteSpace(conscrito.ZonaResidencia))
        {
            TextoFeedbackCadastroConscrito.Text = "Preencha os campos obrigatórios das informações básicas para seleção.";
            DefinirEtapaWizard(EtapaWizardInformacoesBasicas);
            return false;
        }

        return true;
    }

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
            EtapaWizardBlocoF => SecaoBlocoF,
            EtapaWizardBlocoG => SecaoBlocoG,
            EtapaWizardBlocoH => SecaoBlocoH,
            EtapaWizardBlocoI => SecaoBlocoI,
            EtapaWizardBlocoJ => SecaoBlocoJ,
            EtapaWizardManifestacao => SecaoManifestacaoDesejoServir,
            _ => PainelFormularioCadastro
        };
    }

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

    private Conscrito MontarConscritoPeloFormulario()
    {
        return new Conscrito
        {
            Id = _idConscritoEmEdicao ?? string.Empty,
            Nome = CaixaTextoNomeConscrito.Text.Trim(),
            CPF = CaixaTextoCPF.Text.Trim(),
            RA = CaixaTextoRA.Text.Trim(),
            Situacao = ObterTextoSelecionado(ComboSituacaoConscrito),
            NomeMae = CaixaTextoNomeMae.Text.Trim(),
            DataNascimento = CaixaTextoDataNascimento.Text.Trim(),
            PaisResidencia = CaixaTextoPaisResidencia.Text.Trim(),
            MunicipioResidencia = CaixaTextoMunicipioResidencia.Text.Trim(),
            ZonaResidencia = ObterTextoSelecionado(ComboZonaResidencia),
            Endereco = CaixaTextoEndereco.Text.Trim(),
            Bairro = CaixaTextoBairro.Text.Trim(),
            CEP = CaixaTextoCEP.Text.Trim(),
            Telefone = CaixaTextoTelefone.Text.Trim(),
            Municipio = CaixaTextoMunicipio.Text.Trim(),
            Email = CaixaTextoEmail.Text.Trim(),
            Ocupacao = CaixaTextoOcupacao.Text.Trim(),
            MoraCom = CaixaTextoMoraCom.Text.Trim(),
            EstadoCivil = ObterTextoSelecionado(ComboEstadoCivil),
            PossuiFilhos = ObterTextoSelecionado(ComboPossuiFilhos),
            QuantidadeFilhos = CaixaTextoQuantidadeFilhos.Text.Trim(),
            QuemTrabalhaNaFamilia = CaixaTextoQuemTrabalhaFamilia.Text.Trim(),
            QuemSustentaAFamilia = CaixaTextoQuemSustentaFamilia.Text.Trim(),
            RecebeAuxilioGovernamental = ObterTextoSelecionado(ComboRecebeAuxilioGovernamental),
            SituacaoArrimo = ObterTextoSelecionado(ComboSituacaoArrimo),
            EstudaAtualmente = ObterTextoSelecionado(ComboEstudaAtualmente),
            AnoQueEstaCursandoOuUltimoAnoQueCursou = CaixaTextoAnoEscolar.Text.Trim(),
            TemCursosProfissionalizantes = ObterTextoSelecionado(ComboTemCursosProfissionalizantes),
            QuaisCursosProfissionalizantes = CaixaTextoQuaisCursos.Text.Trim(),
            ComprovaCursosProfissionalizantes = ObterTextoSelecionado(ComboComprovaCursos),
            ExperienciaProfissional = ObterTextoSelecionado(ComboExperienciaProfissional),
            QuaisExperienciasProfissionais = CaixaTextoQuaisExperiencias.Text.Trim(),
            ComprovaExperienciaProfissional = ObterTextoSelecionado(ComboComprovaExperiencia),
            PossuiCNH = ObterTextoSelecionado(ComboPossuiCNH),
            RealizandoCursoParaHabilitacao = ObterTextoSelecionado(ComboCursoHabilitacao),
            CategoriaCNH = CaixaTextoCategoriaCNH.Text.Trim(),
            PrimeiroPadraoPreQualificacao = CaixaTextoPrimeiroPadraoPreQualificacao.Text.Trim(),
            SegundoPadraoPreQualificacao = CaixaTextoSegundoPadraoPreQualificacao.Text.Trim(),
            PraticaEsportes = ObterTextoSelecionado(ComboPraticaEsportes),
            QuaisEsportes = CaixaTextoQuaisEsportes.Text.Trim(),
            EhOuJaFoiFederado = ObterTextoSelecionado(ComboFederado),
            SabeNadar = ObterTextoSelecionado(ComboSabeNadar),
            OQueFazNasHorasDeLazer = CaixaTextoLazer.Text.Trim(),
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
            ComoTranstornoPsiquiatricoAfetaSuaVida = CaixaTextoImpactoTranstornoPsiquiatrico.Text.Trim(),
            JaFoiDetidoPelaPolicia = ObterTextoSelecionado(ComboDetidoPelaPolicia),
            QualFoiAInfracao = CaixaTextoQualInfracao.Text.Trim(),
            OutrosAtosInfracionais = CaixaTextoOutrosAtosInfracionais.Text.Trim(),
            DesejaServir = ObterTextoSelecionado(ComboDesejaServir)
        };
    }

    private static string ObterTextoSelecionado(ComboBox comboBox)
    {
        var texto = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;
        return texto == "Selecione" ? string.Empty : texto;
    }

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

        if (FiltroTrabalha.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => TemTrabalhoDeclarado(conscrito.Ocupacao));
        }

        if (FiltroRecebeAuxilio.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.RecebeAuxilioGovernamental));
        }

        if (FiltroEstuda.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.EstudaAtualmente));
        }

        if (FiltroExperiencia.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.ExperienciaProfissional));
        }

        if (FiltroProblemaSaude.IsChecked == true)
        {
            consulta = consulta.Where(conscrito => RespostaEhSim(conscrito.JaTeveProblemaSaude));
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
        if (FiltroSituacaoIndefinido.IsChecked == true) situacoes.Add("Indefinido");

        return situacoes;
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


}
