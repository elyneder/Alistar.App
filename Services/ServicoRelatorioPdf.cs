using System.IO;
using System.Text;
using Alistar.App.Models;
using Microsoft.Win32;

namespace Alistar.App.Services;

/// <summary>
/// Gera relatorios PDF simples com os dados salvos do conscrito.
/// </summary>
/// <remarks>
/// Este servico foi feito sem pacote externo para deixar o projeto facil de rodar.
/// Ele monta um PDF basico em texto, separado por secoes, o suficiente para baixar
/// a ficha geral ou a ficha medica durante a consulta do conscrito.
/// </remarks>
public static class ServicoRelatorioPdf
{
    /// <summary>
    /// Gera o PDF da ficha geral. Este relatorio sempre pode existir quando o conscrito ja foi salvo.
    /// </summary>
    public static bool GerarRelatorioCadastro(Conscrito conscrito)
    {
        var caminho = EscolherCaminho(conscrito, "cadastro");
        if (string.IsNullOrWhiteSpace(caminho))
        {
            return false;
        }

        var documento = new DocumentoPdfSimples($"Cadastro do Conscrito - {Valor(conscrito.Nome)}");
        documento.AdicionarSecao("Identificação");
        documento.AdicionarCampo("Nome", conscrito.Nome);
        documento.AdicionarCampo("CPF", conscrito.CPF);
        documento.AdicionarCampo("RA", conscrito.RA);
        documento.AdicionarCampo("Situação", conscrito.Situacao);
        documento.AdicionarCampo("Nome da mãe", conscrito.NomeMae);
        documento.AdicionarCampo("Data de nascimento", conscrito.DataNascimento);
        documento.AdicionarCampo("País de residência", conscrito.PaisResidencia);
        documento.AdicionarCampo("Município de residência", conscrito.MunicipioResidencia);
        documento.AdicionarCampo("Zona de residência", conscrito.ZonaResidencia);
        documento.AdicionarCampo("Deseja servir", conscrito.DesejaServir);

        documento.AdicionarSecao("Vida pessoal");
        documento.AdicionarCampo("Endereço", conscrito.Entrevista_Vida_Pessoal?.Endereco);
        documento.AdicionarCampo("Bairro", conscrito.Entrevista_Vida_Pessoal?.Bairro);
        documento.AdicionarCampo("CEP", conscrito.Entrevista_Vida_Pessoal?.CEP);
        documento.AdicionarCampo("Telefone", conscrito.Entrevista_Vida_Pessoal?.Telefone);
        documento.AdicionarCampo("Município", conscrito.Entrevista_Vida_Pessoal?.Municipio);
        documento.AdicionarCampo("E-mail", conscrito.Entrevista_Vida_Pessoal?.Email);
        documento.AdicionarCampo("Ocupação", conscrito.Entrevista_Vida_Pessoal?.Ocupacao);
        documento.AdicionarCampo("Mora com", conscrito.Entrevista_Vida_Pessoal?.MoraCom);
        documento.AdicionarCampo("Estado civil", conscrito.Entrevista_Vida_Pessoal?.EstadoCivil);
        documento.AdicionarCampo("Possui filhos", conscrito.Entrevista_Vida_Pessoal?.PossuiFilhos);
        documento.AdicionarCampo("Quantidade de filhos", conscrito.Entrevista_Vida_Pessoal?.QuantidadeFilhos);
        documento.AdicionarCampo("Quem trabalha na família", conscrito.Entrevista_Vida_Pessoal?.QuemTrabalhaNaFamilia);
        documento.AdicionarCampo("Quem sustenta a família", conscrito.Entrevista_Vida_Pessoal?.QuemSustentaAFamilia);
        documento.AdicionarCampo("Recebe auxílio governamental", conscrito.Entrevista_Vida_Pessoal?.RecebeAuxilioGovernamental);

        documento.AdicionarSecao("Arrimo e escolaridade");
        documento.AdicionarCampo("Situação de arrimo", conscrito.Entrevista_Arrimo_De_Familia?.SituacaoArrimo);
        documento.AdicionarCampo("Estuda atualmente", conscrito.Entrevista_Arrimo_De_Familia?.EstudaAtualmente);
        documento.AdicionarCampo("Ano escolar", conscrito.Entrevista_Arrimo_De_Familia?.AnoQueEstaCursandoOuUltimoAnoQueCursou);

        documento.AdicionarSecao("Cursos, experiência e habilitação");
        documento.AdicionarCampo("Tem cursos profissionalizantes", conscrito.Entrevista_Cursos?.TemCursosProfissionalizantes);
        documento.AdicionarCampo("Quais cursos", conscrito.Entrevista_Cursos?.QuaisCursosProfissionalizantes);
        documento.AdicionarCampo("Comprova cursos", conscrito.Entrevista_Cursos?.ComprovaCursosProfissionalizantes);
        documento.AdicionarCampo("Experiência profissional", conscrito.Entrevista_Experiencia?.ExperienciaProfissional);
        documento.AdicionarCampo("Quais experiências", conscrito.Entrevista_Experiencia?.QuaisExperienciasProfissionais);
        documento.AdicionarCampo("Comprova experiência", conscrito.Entrevista_Experiencia?.ComprovaExperienciaProfissional);
        documento.AdicionarCampo("Possui CNH", conscrito.Entrevista_Habilitacao?.PossuiCNH);
        documento.AdicionarCampo("Curso para habilitação", conscrito.Entrevista_Habilitacao?.RealizandoCursoParaHabilitacao);
        documento.AdicionarCampo("Categoria CNH", conscrito.Entrevista_Habilitacao?.CategoriaCNH);

        documento.AdicionarSecao("Pré-qualificação, esportes e lazer");
        documento.AdicionarCampo("Primeiro padrão", conscrito.Entrevista_Pre_Qualificacao_Imediata?.PrimeiroPadraoPreQualificacao);
        documento.AdicionarCampo("Segundo padrão", conscrito.Entrevista_Pre_Qualificacao_Imediata?.SegundoPadraoPreQualificacao);
        documento.AdicionarCampo("Pratica esportes", conscrito.Entrevista_Esportes?.PraticaEsportes);
        documento.AdicionarCampo("Quais esportes", conscrito.Entrevista_Esportes?.QuaisEsportes);
        documento.AdicionarCampo("Federado", conscrito.Entrevista_Esportes?.EhOuJaFoiFederado);
        documento.AdicionarCampo("Sabe nadar", conscrito.Entrevista_Esportes?.SabeNadar);
        documento.AdicionarCampo("Lazer", conscrito.Entrevista_Lazer?.OQueFazNasHorasDeLazer);

        documento.AdicionarSecao("Saúde declarada");
        documento.AdicionarCampo("Problema de saúde", conscrito.Entrevista_Saude?.JaTeveProblemaSaude);
        documento.AdicionarCampo("Qual problema", conscrito.Entrevista_Saude?.QualProblemaSaude);
        documento.AdicionarCampo("Remédio controlado", conscrito.Entrevista_Saude?.UsaRemedioControlado);
        documento.AdicionarCampo("Qual remédio", conscrito.Entrevista_Saude?.QualRemedioControlado);
        documento.AdicionarCampo("Internação psiquiátrica", conscrito.Entrevista_Saude?.JaEsteveInternadoHospitalOuClinicaPsiquiatrica);
        documento.AdicionarCampo("Fuma", conscrito.Entrevista_Saude?.Fuma);
        documento.AdicionarCampo("Bebida alcoólica", conscrito.Entrevista_Saude?.FazUsoBebidaAlcoolica);
        documento.AdicionarCampo("Experimentou drogas", conscrito.Entrevista_Saude?.JaExperimentouDrogas);
        documento.AdicionarCampo("Ainda usa droga", conscrito.Entrevista_Saude?.AindaFazUsoDroga);

        documento.AdicionarSecao("Infrações");
        documento.AdicionarCampo("Já foi detido pela polícia", conscrito.Entrevista_Infracao?.JaFoiDetidoPelaPolicia);
        documento.AdicionarCampo("Qual infração", conscrito.Entrevista_Infracao?.QualFoiAInfracao);
        documento.AdicionarCampo("Outros atos infracionais", conscrito.Entrevista_Infracao?.OutrosAtosInfracionais);

        documento.Salvar(caminho);
        return true;
    }

    /// <summary>
    /// Gera o PDF medico, mas somente quando existe algum dado medico salvo.
    /// </summary>
    public static bool GerarRelatorioMedico(Conscrito conscrito)
    {
        if (!PossuiRelatorioMedico(conscrito))
        {
            return false;
        }

        var caminho = EscolherCaminho(conscrito, "medico");
        if (string.IsNullOrWhiteSpace(caminho))
        {
            return false;
        }

        var entrevista = conscrito.Entrevista_Medica!;
        var documento = new DocumentoPdfSimples($"Relatório Médico - {Valor(conscrito.Nome)}");
        documento.AdicionarSecao("Identificação");
        documento.AdicionarCampo("Nome", conscrito.Nome);
        documento.AdicionarCampo("CPF", conscrito.CPF);
        documento.AdicionarCampo("RA", conscrito.RA);
        documento.AdicionarCampo("Situação", conscrito.Situacao);

        documento.AdicionarSecao("Avaliação física");
        documento.AdicionarCampo("Altura", entrevista.Altura);
        documento.AdicionarCampo("Peso", entrevista.Peso);
        documento.AdicionarCampo("Problema de postura", entrevista.ProblemaPostura);
        documento.AdicionarCampo("Observação de postura", entrevista.ObservacaoProblemaPostura);

        documento.AdicionarSecao("Visão e audição");
        documento.AdicionarCampo("Dificuldade visual ou precisa de óculos", entrevista.DificuldadeVisualOuPrecisaOculos);
        documento.AdicionarCampo("Teste auditivo alterado", entrevista.TesteAuditivoAlterado);
        documento.AdicionarCampo("Observação auditiva", entrevista.ObservacaoTesteAuditivo);

        documento.AdicionarSecao("Exame geral");
        documento.AdicionarCampo("Pressão arterial", entrevista.PressaoArterial);
        documento.AdicionarCampo("Frequência cardíaca", entrevista.FrequenciaCardiaca);
        documento.AdicionarCampo("Respiração", entrevista.Respiracao);

        documento.AdicionarSecao("Histórico médico");
        documento.AdicionarCampo("Família tem doenças graves", entrevista.FamiliaTemDoencasGraves);
        documento.AdicionarCampo("Problema cardíaco ou respiratório", entrevista.JaTeveProblemaCardiacoOuRespiratorio);

        documento.AdicionarSecao("Saúde mental");
        documento.AdicionarCampo("Ansiedade, depressão ou acompanhamento psicológico", entrevista.JaTeveAnsiedadeDepressaoOuAcompanhamentoPsicologico);
        documento.AdicionarCampo("Dificuldade para dormir", entrevista.TemDificuldadeParaDormir);

        documento.Salvar(caminho);
        return true;
    }

    /// <summary>
    /// Verifica se vale mostrar o botao "PDF Medico" na tela.
    /// </summary>
    public static bool PossuiRelatorioMedico(Conscrito conscrito)
    {
        var entrevista = conscrito.Entrevista_Medica;
        return entrevista is not null &&
               (!string.IsNullOrWhiteSpace(entrevista.Altura) ||
                !string.IsNullOrWhiteSpace(entrevista.Peso) ||
                !string.IsNullOrWhiteSpace(entrevista.PressaoArterial) ||
                !string.IsNullOrWhiteSpace(entrevista.FrequenciaCardiaca) ||
                !string.IsNullOrWhiteSpace(entrevista.Respiracao) ||
                !string.IsNullOrWhiteSpace(entrevista.ProblemaPostura) ||
                !string.IsNullOrWhiteSpace(entrevista.DificuldadeVisualOuPrecisaOculos) ||
                !string.IsNullOrWhiteSpace(entrevista.TesteAuditivoAlterado));
    }

    private static string? EscolherCaminho(Conscrito conscrito, string tipoRelatorio)
    {
        var nomeArquivo = $"{LimparNomeArquivo(conscrito.Nome)}_{tipoRelatorio}.pdf";
        var dialogo = new SaveFileDialog
        {
            Title = "Salvar relatório PDF",
            FileName = nomeArquivo,
            Filter = "Arquivo PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialogo.ShowDialog() == true ? dialogo.FileName : null;
    }

    private static string LimparNomeArquivo(string valor)
    {
        var nome = string.IsNullOrWhiteSpace(valor) ? "conscrito" : valor.Trim();
        foreach (var caractere in Path.GetInvalidFileNameChars())
        {
            nome = nome.Replace(caractere, '_');
        }

        return nome.Replace(' ', '_');
    }

    private static string Valor(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? "Não informado" : valor.Trim();
    }

    private sealed class DocumentoPdfSimples
    {
        // Tamanho A4 em pontos. PDF trabalha com uma unidade propria chamada point.
        private const double LarguraPagina = 595;
        private const double AlturaPagina = 842;
        private const double MargemEsquerda = 48;
        private const double MargemTopo = 56;
        private const double AlturaLinha = 14;
        private readonly List<List<string>> _paginas = [[]];
        private readonly string _titulo;
        private double _yAtual = AlturaPagina - MargemTopo;

        public DocumentoPdfSimples(string titulo)
        {
            _titulo = titulo;
            AdicionarLinha(titulo.ToUpperInvariant());
            AdicionarLinha($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
            AdicionarLinha(string.Empty);
        }

        public void AdicionarSecao(string titulo)
        {
            AdicionarLinha(string.Empty);
            AdicionarLinha(titulo.ToUpperInvariant());
        }

        public void AdicionarCampo(string rotulo, string? valor)
        {
            foreach (var linha in QuebrarLinhas($"{rotulo}: {Valor(valor)}", 92))
            {
                AdicionarLinha(linha);
            }
        }

        public void Salvar(string caminho)
        {
            // A estrutura do PDF e uma lista de objetos: catalogo, paginas, fonte e conteudo.
            // Aqui montamos esses objetos manualmente e escrevemos o xref no final.
            var objetos = new List<byte[]>();
            var paginasIds = new List<int>();
            var conteudosIds = new List<int>();

            objetos.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));
            objetos.Add([]);
            objetos.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));

            foreach (var pagina in _paginas)
            {
                var conteudo = CriarConteudoPagina(pagina);
                var conteudoBytes = Encoding.Latin1.GetBytes(conteudo);
                var paginaId = objetos.Count + 1;
                var conteudoId = objetos.Count + 2;

                paginasIds.Add(paginaId);
                conteudosIds.Add(conteudoId);
                objetos.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {LarguraPagina} {AlturaPagina}] /Resources << /Font << /F1 3 0 R >> >> /Contents {conteudoId} 0 R >>"));
                objetos.Add(Bytes($"<< /Length {conteudoBytes.Length} >>\nstream\n{conteudo}\nendstream"));
            }

            objetos[1] = Bytes($"<< /Type /Pages /Kids [{string.Join(" ", paginasIds.Select(id => $"{id} 0 R"))}] /Count {paginasIds.Count} >>");

            using var stream = File.Create(caminho);
            Escrever(stream, "%PDF-1.4\n");
            var offsets = new List<long> { 0 };

            for (var indice = 0; indice < objetos.Count; indice++)
            {
                offsets.Add(stream.Position);
                Escrever(stream, $"{indice + 1} 0 obj\n");
                stream.Write(objetos[indice]);
                Escrever(stream, "\nendobj\n");
            }

            var inicioXref = stream.Position;
            Escrever(stream, $"xref\n0 {objetos.Count + 1}\n");
            Escrever(stream, "0000000000 65535 f \n");

            foreach (var offset in offsets.Skip(1))
            {
                Escrever(stream, $"{offset:0000000000} 00000 n \n");
            }

            Escrever(stream, $"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{inicioXref}\n%%EOF");
        }

        private static string CriarConteudoPagina(List<string> linhas)
        {
            var builder = new StringBuilder();
            var y = AlturaPagina - MargemTopo;

            foreach (var linha in linhas)
            {
                builder.Append("BT /F1 10 Tf ");
                builder.Append(FormattableString.Invariant($"{MargemEsquerda} {y} Td "));
                builder.Append('(');
                builder.Append(EscaparTexto(linha));
                builder.AppendLine(") Tj ET");
                y -= AlturaLinha;
            }

            return builder.ToString();
        }

        private void AdicionarLinha(string linha)
        {
            if (_yAtual < 48)
            {
                _paginas.Add([]);
                _yAtual = AlturaPagina - MargemTopo;
                _paginas[^1].Add(_titulo.ToUpperInvariant());
                _paginas[^1].Add(string.Empty);
                _yAtual -= AlturaLinha * 2;
            }

            _paginas[^1].Add(SanitizarTexto(linha));
            _yAtual -= AlturaLinha;
        }

        private static IEnumerable<string> QuebrarLinhas(string texto, int limite)
        {
            texto = SanitizarTexto(texto);
            if (texto.Length <= limite)
            {
                yield return texto;
                yield break;
            }

            var palavras = texto.Split(' ');
            var linha = string.Empty;

            foreach (var palavra in palavras)
            {
                if ((linha.Length + palavra.Length + 1) > limite)
                {
                    yield return linha;
                    linha = palavra;
                }
                else
                {
                    linha = string.IsNullOrEmpty(linha) ? palavra : $"{linha} {palavra}";
                }
            }

            if (!string.IsNullOrWhiteSpace(linha))
            {
                yield return linha;
            }
        }

        private static string SanitizarTexto(string texto)
        {
            return texto
                .Replace('“', '"')
                .Replace('”', '"')
                .Replace('‘', '\'')
                .Replace('’', '\'')
                .Replace("–", "-")
                .Replace("—", "-");
        }

        private static string EscaparTexto(string texto)
        {
            return texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        private static byte[] Bytes(string texto)
        {
            return Encoding.Latin1.GetBytes(texto);
        }

        private static void Escrever(Stream stream, string texto)
        {
            stream.Write(Bytes(texto));
        }
    }
}
