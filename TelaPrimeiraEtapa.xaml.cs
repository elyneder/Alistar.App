using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaPrimeiraEtapa : Window
{
    private readonly Conscrito? _conscritoInicial;
    private readonly bool _abrirListaAoIniciar;
    private string? _idConscritoEmEdicao;
    private List<Conscrito> _conscritosCarregados = [];

    public TelaPrimeiraEtapa(Conscrito? conscrito = null, bool abrirListaAoIniciar = false)
    {
        _conscritoInicial = conscrito;
        _abrirListaAoIniciar = abrirListaAoIniciar;
        InitializeComponent();
        CarregarConscritos();
        PrepararTela();
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
        Close();
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        MostrarListaConscritos();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            MessageBox.Show(
                "Apenas o usuário admin@alistar.com pode cadastrar um novo entrevistador.",
                "Acesso negado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

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
        var conscrito = MontarConscritoPeloFormulario();

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

        DialogResult = true;
        Close();
    }

    private void LimparFormularioBotao_Click(object sender, RoutedEventArgs e)
    {
        if (EmModoEdicao)
        {
            DialogResult = false;
            Close();
            return;
        }

        LimparCamposFormulario();
        TextoFeedbackCadastroConscrito.Text = string.Empty;
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
        DialogResult = true;
        Close();
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
        TextoTituloFormulario.Text = "Primeira Etapa de Seleção";
        TextoDescricaoFormulario.Text = "Preencha a ficha completa do conscrito. As respostas serão salvas no sistema para consulta posterior.";
        BotaoSalvarConscrito.Content = "Salvar Ficha";
        BotaoLimparFormulario.Content = "Limpar";
        BotaoExcluirConscrito.Visibility = Visibility.Collapsed;
        ComboSituacaoConscrito.SelectedIndex = 0;
        TextoFeedbackCadastroConscrito.Text = string.Empty;
    }

    private void CarregarConscritoParaEdicao(Conscrito conscrito)
    {
        _idConscritoEmEdicao = conscrito.Id;
        LimparCamposFormulario();

        TextoTituloFormulario.Text = "Detalhes do Conscrito";
        TextoDescricaoFormulario.Text = "Revise as informações, edite os dados necessários, altere a situação e salve as mudanças.";
        BotaoSalvarConscrito.Content = "Salvar Alterações";
        BotaoLimparFormulario.Content = "Fechar";
        BotaoExcluirConscrito.Visibility = Visibility.Visible;

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
