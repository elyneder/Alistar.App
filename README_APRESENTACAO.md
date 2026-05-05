# Alistar.App - Guia rapido para apresentacao

Este projeto e um sistema WPF em C# para organizar o fluxo de selecao de conscritos. A ideia principal e simples: o sistema guarda os dados em arquivos JSON, mostra esses dados em telas separadas por etapas e ajuda a consultar, editar, reavaliar e classificar os candidatos.

## Como o projeto esta organizado

- `Models/`: ficam as classes que representam os dados. Por exemplo, `Conscrito` e o objeto principal, e as classes como `VidaPessoal`, `Saude`, `Cursos` e `EntrevistaMedica` guardam blocos especificos do formulario.
- `Services/`: ficam as regras que nao pertencem diretamente a uma tela. Aqui entram login, criptografia de senha, leitura/gravação dos JSONs, geracao de PDF e regras de ranking.
- `Views/`: ficam as telas do sistema. Cada tela tem um arquivo `.xaml`, que monta o visual, e um `.xaml.cs`, que trata os cliques, validacoes e chamadas aos services.
- `conscritos.json`: arquivo local que funciona como banco de dados dos conscritos.
- `entrevistadores.json`: arquivo local que guarda os usuarios do sistema.

## Fluxo das etapas

1. **Login**: o usuario entra com e-mail e senha. O sistema valida no `ServicoAutenticacao`.
2. **Painel de Controle**: e o hub do sistema. Mostra as etapas, a lista de conscritos e os atalhos administrativos.
3. **Primeira Etapa**: cadastra a ficha geral do conscrito. Tambem permite consultar, editar, excluir e gerar PDF do cadastro.
4. **Segunda Etapa**: registra a avaliacao medica inicial. Ela nao deve reabrir a ficha medica antiga para editar; isso ficou para a quarta etapa.
5. **Terceira Etapa**: usa a primeira etapa em modo de entrevista tecnica, para revisar e atualizar a ficha geral.
6. **Quarta Etapa**: reavaliacao medica. Busca por RA, carrega a ficha medica completa e permite editar/salvar.
7. **Quinta Etapa**: ranking final. Ela lista apenas `TG` e `Substituto`, separa os 50 melhores e deixa inapto/dispensado fora.

## Regras importantes

- O administrador e reconhecido pelo e-mail `admin@alistar.com`.
- So administrador pode cadastrar entrevistadores e ver a lista de entrevistadores.
- O entrevistador comum nao ve essas opcoes administrativas.
- Senhas nao ficam em texto puro: elas sao salvas como hash usando BCrypt.
- O sistema salva dados em JSON porque e um projeto local e simples, sem banco de dados externo.
- O PDF e gerado pelo proprio projeto, em texto organizado por secoes.

## Como explicar a estrutura de dados

O `Conscrito` e como se fosse a pasta principal da pessoa. Dentro dele existem dados diretos, como nome, CPF, RA e situacao. Depois existem objetos menores, que sao as abas/blocos do formulario:

- `Entrevista_Vida_Pessoal`: endereco, telefone, ocupacao, familia.
- `Entrevista_Arrimo_De_Familia`: arrimo e escolaridade.
- `Entrevista_Cursos`: cursos profissionalizantes.
- `Entrevista_Experiencia`: experiencia de trabalho.
- `Entrevista_Habilitacao`: CNH e habilitacao.
- `Entrevista_Esportes`: esportes e natacao.
- `Entrevista_Saude`: saude declarada na primeira etapa.
- `Entrevista_Infracao`: historico de infracoes.
- `Entrevista_Medica`: dados da avaliacao medica.

Essa divisao ajuda porque cada parte do formulario fica em uma classe separada, em vez de jogar todos os campos em um arquivo gigante sem organizacao.

## Arquivos principais para citar na apresentacao

- `Models/Conscrito.cs`: modelo principal dos dados.
- `Services/ServicoArmazenamentoConscritos.cs`: le e salva os conscritos no JSON.
- `Services/ServicoAutenticacao.cs`: controla login, permissoes e entrevistadores.
- `Services/ServicoRelatorioPdf.cs`: gera os PDFs de cadastro e medico.
- `Views/TelaPrimeiraEtapa.xaml.cs`: cadastro geral, consulta e PDF.
- `Views/TelaSegundaEtapa.xaml.cs`: avaliacao medica e reavaliacao quando aberta pela quarta etapa.
- `Views/TelaQuintaEtapa.xaml.cs`: ranking dos melhores conscritos.

## Resumo para falar em sala

"Nosso sistema foi feito em C# com WPF. A gente separou o projeto em modelos, servicos e telas. Os modelos representam os dados do conscrito, os servicos fazem as regras principais, como salvar JSON, autenticar usuario e gerar PDF, e as telas cuidam da interacao com o usuario. O sistema tem controle de administrador e entrevistador, gera relatorios e monta um ranking final com base nas informacoes cadastradas."
