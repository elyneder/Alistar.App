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
    private readonly bool _modoReavaliacaoMedica;
    private List<Conscrito> _conscritosCarregados = [];

    public TelaSegundaEtapa(bool modoReavaliacaoMedica = false)
    {
        _modoReavaliacaoMedica = modoReavaliacaoMedica;

        InitializeComponent();
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

        var estavaNaIdentificacao = _etapaWizardAtual == EtapaWizardIdentificacao;
        var selecionouConscrito = TentarSelecionarConscritoPeloRaDigitado();
        if (_modoReavaliacaoMedica && estavaNaIdentificacao && selecionouConscrito)
        {
            return;
        }

        var nome = CaixaTextoNome.Text.Trim();
        var cpf = CaixaTextoCPF.Text.Trim();
        var ra = CaixaTextoRA.Text.Trim();

        if (_etapaWizardAtual != EtapaWizardConfirmacao)
        {
            if (!ValidarEtapaAtualFormulario(nome, cpf, ra))
            {
                return;
            }

            DefinirEtapaWizard(_etapaWizardAtual + 1);
            return;
        }

        if (!ValidarFormularioCompleto(nome, cpf, ra))
        {
            return;
        }

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

        PreencherDadosMedicos(conscrito);

        if (novoCadastro)
        {
            ServicoArmazenamentoConscritos.Adicionar(conscrito);
        }
        else
        {
            ServicoArmazenamentoConscritos.Atualizar(conscrito);
        }

        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    /// <summary>
    /// Garante que o conscrito foi selecionado/preenchido antes de avancar.
    /// </summary>
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

    /// <summary>
    /// Valida somente os campos da etapa atual do wizard.
    /// </summary>
    private bool ValidarEtapaAtualFormulario(string nome, string cpf, string ra)
    {
        if (_etapaWizardAtual == EtapaWizardIdentificacao && !ValidarIdentificacao(nome, cpf, ra))
        {
            return false;
        }

        var areaValidacao = ObterAreaValidacaoEtapaAtual();
        if (FormularioEstaPreenchido(areaValidacao))
        {
            return true;
        }

        TextoFeedback.Text = "Preencha todos os campos desta etapa antes de avancar.";
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

        TextoFeedback.Text = "Preencha todos os campos do formulario antes de salvar.";
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
        var tituloFormulario = _modoReavaliacaoMedica ? "Reavaliação Médica" : "Segunda Etapa";

        SecaoIdentificacao.Visibility = (_etapaWizardAtual == EtapaWizardIdentificacao || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoAvaliacaoFisica.Visibility = (_etapaWizardAtual == EtapaWizardAvaliacaoFisica || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoVisao.Visibility = (_etapaWizardAtual == EtapaWizardVisao || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoTesteAuditivo.Visibility = (_etapaWizardAtual == EtapaWizardTesteAuditivo || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoExameGeral.Visibility = (_etapaWizardAtual == EtapaWizardExameGeral || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoHistoricoMedico.Visibility = (_etapaWizardAtual == EtapaWizardHistoricoMedico || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;
        SecaoSaudeMental.Visibility = (_etapaWizardAtual == EtapaWizardSaudeMental || mostrarFormularioCompleto) ? Visibility.Visible : Visibility.Collapsed;

        AtualizarIndicadorEtapa(IndicadorEtapaIdentificacao, EtapaWizardIdentificacao);
        AtualizarIndicadorEtapaPorIntervalo(IndicadorEtapaExames, EtapaWizardAvaliacaoFisica, EtapaWizardHistoricoMedico);
        AtualizarIndicadorEtapa(IndicadorEtapaSaudeMental, EtapaWizardSaudeMental);
        AtualizarIndicadorEtapa(IndicadorEtapaConfirmacao, EtapaWizardConfirmacao);

        BotaoVoltarEtapaWizard.Visibility = _etapaWizardAtual == EtapaWizardIdentificacao ? Visibility.Collapsed : Visibility.Visible;
        IconeAvancarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Collapsed : Visibility.Visible;
        IconeSalvarBotaoSalvar.Visibility = _etapaWizardAtual == EtapaWizardConfirmacao ? Visibility.Visible : Visibility.Collapsed;

        switch (_etapaWizardAtual)
        {
            case EtapaWizardIdentificacao:
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = _modoReavaliacaoMedica
                    ? "Busque o RA do conscrito para abrir a ficha médica completa."
                    : "Comece pela identificação do conscrito. Depois avance pelos exames em blocos separados.";
                TextoEtapaWizard.Text = $"Etapa 1 de {TotalEtapasWizard} · Identificação";
                TextoResumoEtapaWizard.Text = _modoReavaliacaoMedica
                    ? "Selecione o conscrito pelo RA para visualizar e editar os exames médicos."
                    : "Preencha nome, CPF e RA para localizar ou criar a ficha médica.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case >= EtapaWizardAvaliacaoFisica and <= EtapaWizardHistoricoMedico:
                var nomeBloco = ObterNomeBlocoWizard(_etapaWizardAtual);
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = "Preencha este bloco médico e avance pela seta para continuar.";
                TextoEtapaWizard.Text = $"Etapa {_etapaWizardAtual + 1} de {TotalEtapasWizard} · {nomeBloco}";
                TextoResumoEtapaWizard.Text = "No final, todos os blocos aparecerão juntos para conferência antes de salvar.";
                TextoBotaoSalvar.Text = "Próximo";
                break;

            case EtapaWizardSaudeMental:
                TextoTituloFormulario.Text = tituloFormulario;
                TextoDescricaoFormulario.Text = "Preencha o último bloco médico antes da confirmação final.";
                TextoEtapaWizard.Text = $"Etapa 7 de {TotalEtapasWizard} · Saúde mental";
                TextoResumoEtapaWizard.Text = "Após esta etapa, a ficha completa aparecerá para revisão.";
                TextoBotaoSalvar.Text = "Ir para confirmação";
                break;

            default:
                TextoTituloFormulario.Text = _modoReavaliacaoMedica ? "Reavaliando a Pessoa" : "Confirmar Avaliação Médica";
                TextoDescricaoFormulario.Text = _modoReavaliacaoMedica
                    ? "Confira a ficha médica completa. Se precisar, altere os dados e salve a reavaliação."
                    : "Confira a ficha médica completa abaixo. Se precisar, altere qualquer campo antes de salvar.";
                TextoEtapaWizard.Text = $"Etapa 8 de {TotalEtapasWizard} · Confirmação final";
                TextoResumoEtapaWizard.Text = _modoReavaliacaoMedica
                    ? "Ficha completa carregada para confirmação dos exames médicos."
                    : "Todos os blocos estão visíveis para revisão e ajustes finais.";
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
        DefinirEtapaWizard(EtapaWizardIdentificacao);
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        ServicoNavegacao.Trocar(this, new TelaPainelControle());
    }

    /// <summary>
    /// Recarrega conscritos salvos e atualiza o total mostrado no menu lateral.
    /// </summary>
    private void CarregarResumo()
    {
        _conscritosCarregados = ServicoArmazenamentoConscritos.ObterTodos()
            .OrderBy(conscrito => conscrito.RA)
            .ThenBy(conscrito => conscrito.Nome)
            .ToList();

        TextoQuantidadeConscritos.Text = _conscritosCarregados.Count.ToString();
        AtualizarResultadosPesquisaRA();
    }

    private void CaixaPesquisaRA_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_atualizandoBuscaConscrito)
        {
            return;
        }

        var raFormatado = FormatarRa(CaixaPesquisaRA.Text);
        if (CaixaPesquisaRA.Text != raFormatado)
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
            .Where(conscrito => !string.IsNullOrWhiteSpace(conscrito.RA) &&
                                (conscrito.RA.Contains(pesquisa, StringComparison.OrdinalIgnoreCase) ||
                                 ObterApenasDigitos(conscrito.RA, 12).Contains(digitosPesquisa, StringComparison.OrdinalIgnoreCase)))
            .Take(8)
            .ToList();

        ListaResultadosPesquisaRA.ItemsSource = resultados;
        PainelResultadosPesquisaRA.Visibility = resultados.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TextoResultadoPesquisaRA.Text = resultados.Count > 0
            ? ObterTextoResultadoEncontrado(resultados.Count)
            : "Nenhum conscrito encontrado para este RA.";
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
        DefinirEtapaWizard(EtapaWizardConfirmacao);
    }

    private string ObterTextoBuscaInicial()
    {
        return _modoReavaliacaoMedica
            ? "Digite o RA para localizar a ficha médica do conscrito."
            : "Digite o RA para localizar o conscrito.";
    }

    private string ObterTextoResultadoEncontrado(int totalResultados)
    {
        return _modoReavaliacaoMedica
            ? $"{totalResultados} resultado(s) encontrado(s). Selecione um conscrito para abrir a ficha médica completa."
            : $"{totalResultados} resultado(s) encontrado(s). Selecione um conscrito para preencher a identificação.";
    }

    private void LimparDadosMedicos()
    {
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
               (!string.IsNullOrWhiteSpace(entrevistaMedica.Altura) ||
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

    private static string ObterTextoSelecionado(ComboBox comboBox)
    {
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
