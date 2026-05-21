using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Alistar.App.Models;
using Alistar.App.Services;

namespace Alistar.App;

internal static class DialogoEntrevistador
{
    private static readonly Brush VerdeEscuro = Cor("#123322");
    private static readonly Brush VerdePrimario = Cor("#145C3B");
    private static readonly Brush TextoSuave = Cor("#63756E");
    private static readonly Brush Borda = Cor("#DDE7E2");
    private static readonly Brush Fundo = Cor("#F4F7F6");

    public static void MostrarEdicao(Window owner, EntrevistadorResumo entrevistador, Action aoSalvar)
    {
        var janela = CriarJanela(owner, "Editar entrevistador", 560, 460);
        var nome = CriarCaixaTexto(entrevistador.Nome);
        var email = CriarCaixaTexto(entrevistador.Email);
        var senha = new PasswordBox
        {
            Padding = new Thickness(12, 9, 12, 9),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            FontSize = 13
        };
        var feedback = new TextBlock
        {
            Foreground = Cor("#C0392B"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var corpo = new StackPanel();
        corpo.Children.Add(CriarCabecalho(janela, "Editar entrevistador", "Atualize nome, e-mail ou defina uma nova senha."));
        corpo.Children.Add(CriarRotulo("Nome"));
        corpo.Children.Add(CriarCampo(nome));
        corpo.Children.Add(CriarRotulo("E-mail"));
        corpo.Children.Add(CriarCampo(email));
        corpo.Children.Add(CriarRotulo("Nova senha (opcional)"));
        corpo.Children.Add(CriarCampo(senha));
        corpo.Children.Add(feedback);

        var botoes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        };
        var voltar = CriarBotaoSecundario("\uE72B", "Voltar");
        var salvar = CriarBotaoPrimario("\uE74E", "Salvar");
        voltar.Click += (_, _) => janela.Close();
        salvar.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nome.Text) || string.IsNullOrWhiteSpace(email.Text))
            {
                feedback.Text = "Preencha nome e e-mail.";
                return;
            }

            var resultado = ServicoAutenticacao.AtualizarEntrevistador(
                entrevistador.Email,
                nome.Text,
                email.Text,
                senha.Password);

            if (resultado == ResultadoCadastroUsuario.EmailJaCadastrado)
            {
                feedback.Text = "Já existe um usuário com este e-mail.";
                return;
            }

            if (resultado != ResultadoCadastroUsuario.Sucesso)
            {
                feedback.Text = "Não foi possível atualizar este entrevistador.";
                return;
            }

            janela.Close();
            aoSalvar();
        };

        botoes.Children.Add(voltar);
        botoes.Children.Add(salvar);
        corpo.Children.Add(botoes);
        janela.Content = CriarCartao(corpo);
        janela.ShowDialog();
    }

    public static void MostrarDetalhes(Window owner, EntrevistadorResumo entrevistador)
    {
        var logs = ServicoAuditoria.ObterTodos()
            .Where(log => string.Equals(log.UsuarioEmail, entrevistador.Email, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var janela = CriarJanela(owner, $"Detalhes de {entrevistador.Nome}", 980, 620);
        var conteudo = new Grid();
        conteudo.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        conteudo.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        conteudo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        conteudo.Children.Add(CriarCabecalho(janela, entrevistador.Nome, $"{entrevistador.Email} · {logs.Count} registro(s) de histórico"));

        var resumo = new Border
        {
            Background = Cor("#EAF1ED"),
            BorderBrush = Borda,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = logs.Count == 0
                    ? "Ainda não há registros de histórico para este entrevistador."
                    : "Histórico de ações registradas para este entrevistador.",
                Foreground = VerdeEscuro,
                FontWeight = FontWeights.SemiBold
            }
        };
        Grid.SetRow(resumo, 1);
        conteudo.Children.Add(resumo);

        var grade = CriarGradeHistorico(logs);
        Grid.SetRow(grade, 2);
        conteudo.Children.Add(grade);

        janela.Content = CriarCartao(conteudo);
        janela.ShowDialog();
    }

    private static Window CriarJanela(Window owner, string titulo, double largura, double altura)
    {
        return new Window
        {
            Title = titulo,
            Owner = owner,
            Width = largura,
            Height = altura,
            MinWidth = Math.Min(largura, 520),
            MinHeight = Math.Min(altura, 420),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            Background = Fundo,
            Icon = owner.Icon
        };
    }

    private static Border CriarCartao(UIElement conteudo)
    {
        return new Border
        {
            Background = Brushes.White,
            BorderBrush = Borda,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24),
            Margin = new Thickness(18),
            Child = conteudo
        };
    }

    private static Grid CriarCabecalho(Window janela, string titulo, string subtitulo)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 22) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textos = new StackPanel();
        textos.Children.Add(new TextBlock
        {
            Text = titulo,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = VerdeEscuro
        });
        textos.Children.Add(new TextBlock
        {
            Text = subtitulo,
            FontSize = 13,
            Foreground = TextoSuave,
            Margin = new Thickness(0, 4, 0, 0)
        });
        grid.Children.Add(textos);

        var voltar = CriarBotaoSecundario("\uE72B", "Voltar");
        voltar.Margin = new Thickness(16, 0, 0, 0);
        voltar.Click += (_, _) => janela.Close();
        Grid.SetColumn(voltar, 1);
        grid.Children.Add(voltar);

        return grid;
    }

    private static TextBlock CriarRotulo(string texto)
    {
        return new TextBlock
        {
            Text = texto,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = VerdeEscuro,
            Margin = new Thickness(0, 0, 0, 5)
        };
    }

    private static TextBox CriarCaixaTexto(string texto)
    {
        return new TextBox
        {
            Text = texto,
            Padding = new Thickness(12, 9, 12, 9),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = Cor("#1F2D26"),
            FontSize = 13
        };
    }

    private static Border CriarCampo(Control controle)
    {
        return new Border
        {
            Background = Brushes.White,
            BorderBrush = Borda,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 14),
            Child = controle
        };
    }

    private static Button CriarBotaoPrimario(string icone, string texto)
    {
        var botao = CriarBotaoComIcone(icone, texto);
        botao.Style = Application.Current.TryFindResource("BotaoPrimarioStyle") as Style;
        botao.Background = VerdePrimario;
        botao.Foreground = Brushes.White;
        botao.BorderThickness = new Thickness(0);
        return botao;
    }

    private static Button CriarBotaoSecundario(string icone, string texto)
    {
        var botao = CriarBotaoComIcone(icone, texto);
        botao.Style = Application.Current.TryFindResource("BotaoSecundarioStyle") as Style;
        botao.Background = Cor("#EAF1ED");
        botao.Foreground = VerdePrimario;
        botao.BorderBrush = Borda;
        botao.BorderThickness = new Thickness(1);
        return botao;
    }

    private static Button CriarBotaoComIcone(string icone, string texto)
    {
        return new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = icone, FontFamily = new FontFamily("Segoe MDL2 Assets"), Margin = new Thickness(0, 0, 8, 0) },
                    new TextBlock { Text = texto }
                }
            },
            Height = 40,
            MinWidth = 104,
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(10, 0, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontWeight = FontWeights.SemiBold
        };
    }

    private static DataGrid CriarGradeHistorico(IEnumerable<RegistroAuditoria> logs)
    {
        var grade = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            ItemsSource = logs,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = Cor("#EEF3F0"),
            BorderBrush = Borda,
            BorderThickness = new Thickness(1),
            RowHeight = 38,
            RowBackground = Brushes.White,
            AlternatingRowBackground = Cor("#FAFCFB")
        };

        grade.Resources.Add(SystemColors.HighlightBrushKey, Cor("#E3F5EA"));
        grade.Resources.Add(SystemColors.HighlightTextBrushKey, VerdeEscuro);
        grade.Resources.Add(SystemColors.InactiveSelectionHighlightBrushKey, Cor("#E3F5EA"));
        grade.Resources.Add(SystemColors.InactiveSelectionHighlightTextBrushKey, VerdeEscuro);
        grade.Columns.Add(new DataGridTextColumn { Header = "Data", Binding = new Binding("DataHora") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = 140 });
        grade.Columns.Add(new DataGridTextColumn { Header = "Ação", Binding = new Binding("Acao"), Width = 120 });
        grade.Columns.Add(new DataGridTextColumn { Header = "Entidade", Binding = new Binding("Entidade"), Width = 180 });
        grade.Columns.Add(new DataGridTextColumn { Header = "Campo", Binding = new Binding("Campo"), Width = 150 });
        grade.Columns.Add(new DataGridTextColumn { Header = "Descrição", Binding = new Binding("Descricao"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        return grade;
    }

    private static SolidColorBrush Cor(string valor)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(valor)!);
    }
}
