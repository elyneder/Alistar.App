using System.IO;
using System.Text;
using Alistar.App.Models;
using Microsoft.Win32;

namespace Alistar.App.Services;

/// <summary>
/// Gera relatórios PDF compactos e profissionais com os dados salvos do conscrito.
/// </summary>
public static class ServicoRelatorioPdf
{
    public static bool GerarRelatorioCadastro(Conscrito conscrito)
    {
        var caminho = EscolherCaminho(conscrito, "cadastro");
        if (string.IsNullOrWhiteSpace(caminho))
        {
            return false;
        }

        var documento = new DocumentoPdfSimples($"Ficha de Entrevista - {Valor(conscrito.Nome)}");
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
        documento.AdicionarCampo("Peso", conscrito.Peso);
        documento.AdicionarCampo("Altura", conscrito.Altura);
        documento.AdicionarCampo("Tamanho da cabeça", conscrito.TamanhoCabeca);
        documento.AdicionarCampo("Tamanho do calçado", conscrito.TamanhoCalcado);
        documento.AdicionarCampo("Deseja servir", conscrito.DesejaServir);
        documento.AdicionarCampo("Observação", conscrito.Observacao);

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
        documento.AdicionarCampo("Qual auxílio governamental", conscrito.Entrevista_Vida_Pessoal?.QualAuxilioGovernamental);

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
        documento.AdicionarCampo("Para que usa remédio", conscrito.Entrevista_Saude?.ParaQueUsaRemedioControlado);
        documento.AdicionarCampo("Há quanto tempo usa remédio", conscrito.Entrevista_Saude?.HaQuantoTempoUsaRemedioControlado);
        documento.AdicionarCampo("Por quanto tempo ainda usará", conscrito.Entrevista_Saude?.PorQuantoTempoAindaUsaraRemedio);
        documento.AdicionarCampo("Internação psiquiátrica", conscrito.Entrevista_Saude?.JaEsteveInternadoHospitalOuClinicaPsiquiatrica);
        documento.AdicionarCampo("Motivo da internação", conscrito.Entrevista_Saude?.MotivoInternacao);
        documento.AdicionarCampo("Tempo de internação", conscrito.Entrevista_Saude?.TempoInternacao);
        documento.AdicionarCampo("Fuma", conscrito.Entrevista_Saude?.Fuma);
        documento.AdicionarCampo("Há quanto tempo fuma", conscrito.Entrevista_Saude?.HaQuantoTempoFuma);
        documento.AdicionarCampo("Bebida alcoólica", conscrito.Entrevista_Saude?.FazUsoBebidaAlcoolica);
        documento.AdicionarCampo("Frequência da bebida", conscrito.Entrevista_Saude?.FrequenciaBebidaAlcoolica);
        documento.AdicionarCampo("Experimentou drogas", conscrito.Entrevista_Saude?.JaExperimentouDrogas);
        documento.AdicionarCampo("Qual droga", conscrito.Entrevista_Saude?.QualDroga);
        documento.AdicionarCampo("Ainda usa droga", conscrito.Entrevista_Saude?.AindaFazUsoDroga);
        documento.AdicionarCampo("Frequência de uso de droga", conscrito.Entrevista_Saude?.FrequenciaUsoDroga);
        documento.AdicionarCampo("Última vez que utilizou droga", conscrito.Entrevista_Saude?.QuandoFoiUltimaVezQueUtilizouDroga);
        documento.AdicionarCampo("Parente usuário de drogas", conscrito.Entrevista_Saude?.PossuiParenteUsuarioDrogas);
        documento.AdicionarCampo("Quem é o parente usuário", conscrito.Entrevista_Saude?.QuemParenteUsuarioDrogas);
        documento.AdicionarCampo("Impacto do uso por parente", conscrito.Entrevista_Saude?.ComoParenteUsuarioDrogasAfetaSuaVida);
        documento.AdicionarCampo("Parente com transtorno psiquiátrico", conscrito.Entrevista_Saude?.PossuiParenteComHistoricoTranstornoPsiquiatrico);
        documento.AdicionarCampo("Quem é o parente com transtorno", conscrito.Entrevista_Saude?.QuemParenteComHistoricoTranstornoPsiquiatrico);
        documento.AdicionarCampo("Impacto do transtorno psiquiátrico", conscrito.Entrevista_Saude?.ComoTranstornoPsiquiatricoAfetaSuaVida);

        documento.AdicionarSecao("Infrações");
        documento.AdicionarCampo("Já foi detido pela polícia", conscrito.Entrevista_Infracao?.JaFoiDetidoPelaPolicia);
        documento.AdicionarCampo("Qual infração", conscrito.Entrevista_Infracao?.QualFoiAInfracao);
        documento.AdicionarCampo("Outros atos infracionais", conscrito.Entrevista_Infracao?.OutrosAtosInfracionais);

        documento.Salvar(caminho);
        return true;
    }

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
        private const double LarguraPagina = 595;
        private const double AlturaPagina = 842;
        private const double Margem = 36;
        private const double TopoConteudo = 738;
        private const double Rodape = 34;
        private const double LarguraColuna = 252;
        private const double EspacoColunas = 19;
        private readonly List<SecaoPdf> _secoes = [];
        private readonly string _titulo;
        private SecaoPdf? _secaoAtual;

        public DocumentoPdfSimples(string titulo)
        {
            _titulo = titulo;
        }

        public void AdicionarSecao(string titulo)
        {
            _secaoAtual = new SecaoPdf(SanitizarTexto(titulo));
            _secoes.Add(_secaoAtual);
        }

        public void AdicionarCampo(string rotulo, string? valor)
        {
            _secaoAtual ??= new SecaoPdf("Dados");
            if (!_secoes.Contains(_secaoAtual))
            {
                _secoes.Add(_secaoAtual);
            }

            _secaoAtual.Campos.Add(new CampoPdf(SanitizarTexto(rotulo), Valor(valor)));
        }

        public void Salvar(string caminho)
        {
            var paginas = CriarPaginas(7.4, 9.2);
            if (paginas.Count > 2)
            {
                paginas = CriarPaginas(6.6, 8.2);
            }

            if (paginas.Count > 2)
            {
                paginas = CriarPaginas(6.0, 7.4, truncar: true);
            }

            SalvarPaginas(caminho, paginas.Take(2).ToList());
        }

        private List<string> CriarPaginas(double tamanhoFonte, double alturaLinha, bool truncar = false)
        {
            var paginas = new List<StringBuilder> { new() };
            var paginaAtual = 0;
            var colunaAtual = 0;
            var y = TopoConteudo;
            var limiteCaracteres = Math.Max(44, (int)(LarguraColuna / (tamanhoFonte * 0.47)));

            AdicionarCabecalho(paginas[paginaAtual], 1);

            foreach (var secao in _secoes.Where(secao => secao.Campos.Count > 0))
            {
                GarantirEspaco(18, ref paginaAtual, ref colunaAtual, ref y, paginas);
                AdicionarFaixaSecao(paginas[paginaAtual], ObterXColuna(colunaAtual), y, secao.Titulo, tamanhoFonte);
                y -= 18;

                foreach (var campo in secao.Campos)
                {
                    var linhas = QuebrarLinhas($"{campo.Rotulo}: {campo.Valor}", limiteCaracteres, truncar).ToList();
                    var alturaCampo = Math.Max(alturaLinha, linhas.Count * alturaLinha);
                    GarantirEspaco(alturaCampo, ref paginaAtual, ref colunaAtual, ref y, paginas);

                    foreach (var linha in linhas)
                    {
                        AdicionarTexto(paginas[paginaAtual], ObterXColuna(colunaAtual), y, linha, tamanhoFonte, "F1", "0.09 0.10 0.11");
                        y -= alturaLinha;
                    }
                }

                y -= 4;
            }

            for (var indice = 0; indice < paginas.Count; indice++)
            {
                AdicionarRodape(paginas[indice], indice + 1, paginas.Count);
            }

            return paginas.Select(pagina => pagina.ToString()).ToList();

            void GarantirEspaco(double alturaNecessaria, ref int pagina, ref int coluna, ref double posicaoY, List<StringBuilder> listaPaginas)
            {
                if (posicaoY - alturaNecessaria >= Rodape)
                {
                    return;
                }

                if (coluna == 0)
                {
                    coluna = 1;
                    posicaoY = TopoConteudo;
                    return;
                }

                coluna = 0;
                pagina++;
                listaPaginas.Add(new StringBuilder());
                AdicionarCabecalho(listaPaginas[pagina], pagina + 1);
                posicaoY = TopoConteudo;
            }
        }

        private static void SalvarPaginas(string caminho, List<string> paginas)
        {
            var objetos = new List<byte[]>();
            var paginasIds = new List<int>();

            objetos.Add(Bytes("<< /Type /Catalog /Pages 2 0 R >>"));
            objetos.Add([]);
            objetos.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
            objetos.Add(Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));

            foreach (var pagina in paginas)
            {
                var conteudoBytes = Encoding.Latin1.GetBytes(pagina);
                var paginaId = objetos.Count + 1;
                var conteudoId = objetos.Count + 2;

                paginasIds.Add(paginaId);
                objetos.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {LarguraPagina} {AlturaPagina}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {conteudoId} 0 R >>"));
                objetos.Add(Bytes($"<< /Length {conteudoBytes.Length} >>\nstream\n{pagina}\nendstream"));
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

        private void AdicionarCabecalho(StringBuilder builder, int pagina)
        {
            AdicionarRetangulo(builder, 0, 776, LarguraPagina, 66, "0.04 0.12 0.08");
            AdicionarRetangulo(builder, Margem, 758, LarguraPagina - (Margem * 2), 1.4, "0.10 0.40 0.26");
            AdicionarTexto(builder, Margem, 815, "ALISTAR", 9, "F2", "1 1 1");
            AdicionarTexto(builder, Margem, 795, SanitizarTexto(_titulo).ToUpperInvariant(), 13, "F2", "1 1 1");
            AdicionarTexto(builder, 420, 815, $"Página {pagina}", 7.5, "F1", "0.82 0.91 0.86");
            AdicionarTexto(builder, 420, 797, $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", 7.5, "F1", "0.82 0.91 0.86");
        }

        private static void AdicionarRodape(StringBuilder builder, int pagina, int totalPaginas)
        {
            AdicionarLinha(builder, Margem, 28, LarguraPagina - (Margem * 2), "0.76 0.80 0.78");
            AdicionarTexto(builder, Margem, 18, $"Página {pagina} de {totalPaginas}", 7, "F1", "0.35 0.39 0.37");
            AdicionarTexto(builder, 395, 18, "Documento gerado pelo Sistema Alistar", 7, "F1", "0.35 0.39 0.37");
        }

        private static void AdicionarFaixaSecao(StringBuilder builder, double x, double y, string titulo, double tamanhoFonte)
        {
            AdicionarRetangulo(builder, x, y - 4, LarguraColuna, 13, "0.07 0.07 0.07");
            AdicionarTexto(builder, x + 5, y, titulo.ToUpperInvariant(), Math.Max(6.8, tamanhoFonte), "F2", "1 1 1");
        }

        private static void AdicionarTexto(StringBuilder builder, double x, double y, string texto, double tamanho, string fonte, string cor)
        {
            builder.Append(cor);
            builder.Append(" rg BT /");
            builder.Append(fonte);
            builder.Append(FormattableString.Invariant($" {tamanho:0.##} Tf {x:0.##} {y:0.##} Td "));
            builder.Append('(');
            builder.Append(EscaparTexto(SanitizarTexto(texto)));
            builder.AppendLine(") Tj ET");
        }

        private static void AdicionarRetangulo(StringBuilder builder, double x, double y, double largura, double altura, string cor)
        {
            builder.Append(cor);
            builder.Append(FormattableString.Invariant($" rg {x:0.##} {y:0.##} {largura:0.##} {altura:0.##} re f\n"));
        }

        private static void AdicionarLinha(StringBuilder builder, double x, double y, double largura, string cor)
        {
            builder.Append(cor);
            builder.Append(FormattableString.Invariant($" RG {x:0.##} {y:0.##} m {x + largura:0.##} {y:0.##} l S\n"));
        }

        private static double ObterXColuna(int coluna)
        {
            return Margem + (coluna * (LarguraColuna + EspacoColunas));
        }

        private static IEnumerable<string> QuebrarLinhas(string texto, int limite, bool truncar)
        {
            texto = SanitizarTexto(texto);
            if (truncar && texto.Length > limite)
            {
                yield return texto[..Math.Max(0, limite - 3)] + "...";
                yield break;
            }

            if (texto.Length <= limite)
            {
                yield return texto;
                yield break;
            }

            var palavras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var linha = string.Empty;

            foreach (var palavra in palavras)
            {
                if (palavra.Length > limite)
                {
                    if (!string.IsNullOrWhiteSpace(linha))
                    {
                        yield return linha;
                    }

                    yield return palavra[..limite];
                    linha = string.Empty;
                    continue;
                }

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

        private sealed record SecaoPdf(string Titulo)
        {
            public List<CampoPdf> Campos { get; } = [];
        }

        private sealed record CampoPdf(string Rotulo, string Valor);
    }
}
