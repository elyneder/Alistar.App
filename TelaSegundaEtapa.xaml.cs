using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

public partial class TelaSegundaEtapa : Window
{
    public TelaSegundaEtapa()
    {
        InitializeComponent();
        CarregarResumo();
    }

    private void MostrarTelaInicialBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MostrarListaConscritosBotao_Click(object sender, RoutedEventArgs e)
    {
        var telaPrimeiraEtapa = new TelaPrimeiraEtapa(abrirListaAoIniciar: true)
        {
            Owner = this
        };

        telaPrimeiraEtapa.ShowDialog();
        CarregarResumo();
    }

    private void AbrirCadastroUsuarioBotao_Click(object sender, RoutedEventArgs e)
    {
        if (!ServicoAutenticacao.UsuarioAtualEhAdministrador())
        {
            MessageBox.Show(
                "Apenas o usuario admin@alistar.com pode cadastrar um novo entrevistador.",
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

    private void SalvarBotao_Click(object sender, RoutedEventArgs e)
    {
        TextoFeedback.Foreground = Brushes.Firebrick;

        var nome = CaixaTextoNome.Text.Trim();
        var cpf = CaixaTextoCPF.Text.Trim();
        var ra = CaixaTextoRA.Text.Trim();

        if (string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(cpf) ||
            string.IsNullOrWhiteSpace(ra))
        {
            TextoFeedback.Text = "Preencha nome, CPF e RA para salvar a etapa medica.";
            return;
        }

        var conscritos = ServicoArmazenamentoConscritos.ObterTodos();
        var conscrito = conscritos.FirstOrDefault(item =>
            string.Equals(item.CPF, cpf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.RA, ra, StringComparison.OrdinalIgnoreCase));

        var novoCadastro = conscrito is null;
        conscrito ??= new Conscrito();

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

        TextoFeedback.Foreground = Brushes.ForestGreen;
        TextoFeedback.Text = "Segunda Etapa salva com sucesso.";
        CarregarResumo();
    }

    private void LimparBotao_Click(object sender, RoutedEventArgs e)
    {
        LimparControles(this);
        TextoFeedback.Text = string.Empty;
        TextoFeedback.Foreground = Brushes.Firebrick;
    }

    private void FecharBotao_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CarregarResumo()
    {
        TextoQuantidadeConscritos.Text = ServicoArmazenamentoConscritos.ObterTodos().Count.ToString();
    }

    private void PreencherDadosMedicos(Conscrito conscrito)
    {
        conscrito.Altura = CaixaTextoAltura.Text.Trim();
        conscrito.Peso = CaixaTextoPeso.Text.Trim();
        conscrito.ProblemaPostura = ObterTextoSelecionado(ComboProblemaPostura);
        conscrito.ObservacaoProblemaPostura = CaixaTextoObservacaoPostura.Text.Trim();
        conscrito.DificuldadeVisualOuPrecisaOculos = ObterTextoSelecionado(ComboDificuldadeVisual);
        conscrito.TesteAuditivoAlterado = ObterTextoSelecionado(ComboTesteAuditivo);
        conscrito.ObservacaoTesteAuditivo = CaixaTextoObservacaoAuditiva.Text.Trim();
        conscrito.PressaoArterial = CaixaTextoPressaoArterial.Text.Trim();
        conscrito.FrequenciaCardiaca = CaixaTextoFrequenciaCardiaca.Text.Trim();
        conscrito.Respiracao = CaixaTextoRespiracao.Text.Trim();
        conscrito.FamiliaTemDoencasGraves = ObterTextoSelecionado(ComboDoencasGravesFamilia);
        conscrito.JaTeveProblemaCardiacoOuRespiratorio = ObterTextoSelecionado(ComboProblemaCardiacoRespiratorio);
        conscrito.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico = ObterTextoSelecionado(ComboSaudeMental);
        conscrito.TemDificuldadeParaDormir = ObterTextoSelecionado(ComboDificuldadeDormir);
    }

    private static string ObterTextoSelecionado(ComboBox comboBox)
    {
        var texto = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;
        return texto == "Selecione" ? string.Empty : texto;
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
}
