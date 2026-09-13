# Cadastro de alunos

Aplicação web para gerenciar alunos, com cadastro, consulta, edição e exclusão. Desenvolvida em C# com ASP.NET Core e SQLite, inclui validação de dados, tratamento de erros e testes automatizados.

Criado para o **EC Experience 2026 da faculdade FESA**, o projeto serve como exemplo prático para explorar diferentes conceitos de qualidade de software, como legibilidade, confiabilidade, testes e manutenção.

## Executar

Pré-requisito: **SDK do .NET 10**. Não é necessário instalar Node.js nem um servidor de banco de dados. A primeira restauração precisa de acesso ao NuGet.

Na raiz do repositório:

```sh
dotnet restore
dotnet run --project src/Escola
```

Abra [http://localhost:5080](http://localhost:5080). A interface permite **listar, criar, editar e deletar alunos**, com confirmação antes da exclusão e mensagens de validação.

O SQLite cria automaticamente `src/Escola/dados/escola.db`; os dados permanecem após reiniciar o app. O arquivo não entra no Git. Para usar outro caminho, configure `DatabasePath`:

```sh
dotnet run --project src/Escola -- --DatabasePath=./dados/demo.db
```

Também é possível abrir `FESA-ECExperience-2026.slnx` no Visual Studio com suporte ao .NET 10.

## Regras do cadastro

| Campo | Regra |
| --- | --- |
| RA | Obrigatório, único, apenas dígitos ASCII de 0 a 9. Armazenado como texto para preservar zeros à esquerda. |
| Nome | Obrigatório, nome e sobrenome, até 100 caracteres após normalização de espaços. Pelo menos duas partes devem conter letras. |
| Data de nascimento | Data válida cujo ano seja maior que 1950: a partir de 01/01/1951. |
| Entrada na escola | Data válida e obrigatória. |
| Curso | Enum: Engenharia da Computação, Administração ou Engenharia de Alimentos. |

Todos os campos podem ser editados. Um `Id` interno mantém a identidade do aluno quando o RA muda. RAs como `00123` e `123` são identificadores distintos. Não há restrição de idade máxima, proibição de datas futuras ou validação da relação entre nascimento e entrada.

## Testes

```sh
dotnet test tests/Escola.Tests
```

Executar apenas um nível:

```sh
dotnet test tests/Escola.Tests --filter "FullyQualifiedName!~ApiTests"
dotnet test tests/Escola.Tests --filter "FullyQualifiedName~ApiTests"
```

- **Unitários:** regras de RA, nome, datas e enum; criação, listagem, edição e exclusão; duplicidade, inexistência e preservação dos dados após validação rejeitada.
- **Integração:** requisições HTTP com `WebApplicationFactory`, serialização JSON, status 200/201/204/400/404/409, quatro operações com SQLite real, leitura do arquivo por outra instância, campos ausentes, datas impossíveis, enum inválido e arquivos da interface.

Cada teste de integração cria seu próprio banco em uma pasta temporária e o remove ao terminar. O banco da aplicação não é alterado. Os testes automatizados de integração não controlam o navegador.

Para executar os testes e coletar a cobertura em formato OpenCover:

```sh
dotnet test tests/Escola.Tests --collect:"XPlat Code Coverage;Format=opencover" --logger trx --results-directory TestResults
```

A action do SonarQube executa os testes unitários e de integração e envia a cobertura C# e os resultados dos testes para a análise. Os relatórios também ficam disponíveis no artefato `test-results-and-coverage` da execução no GitHub Actions. Se algum teste falhar ou a cobertura não for gerada, o fluxo falha.

## Testes E2E com Playwright

Os testes E2E usam **Playwright para .NET e xUnit**, escritos em C#, para interagir com a interface e verificar o fluxo completo com a API e o SQLite reais. Cobrem listagem, criação, edição, cancelamento, exclusão com confirmação, RA duplicado e validações de campos. O recarregamento da página verifica que os dados foram persistidos.

Na raiz do projeto, compile os testes e instale o Chromium pelo script gerado pelo pacote NuGet. O comando `pwsh` requer PowerShell 7; não é necessário instalar Node.js ou npm:

```sh
dotnet build tests/Escola.E2E --configuration Release
pwsh tests/Escola.E2E/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test tests/Escola.E2E --configuration Release --no-build --logger trx --logger html --results-directory TestResults/E2E
```

Cada teste inicia um servidor Kestrel em uma porta livre, com banco SQLite e contexto de navegador exclusivos. Ao terminar, encerra o navegador e o servidor. O banco habitual da aplicação não é usado. O diretório `artifacts/e2e/<id>/` guarda o banco de teste, um screenshot final e um trace de cada cenário, inclusive dos aprovados. O caminho aparece na saída do teste e nos relatórios TRX/HTML. Esses arquivos são ignorados pelo Git e podem ser removidos após a execução.
## API

| Método | Rota | Resultado |
| --- | --- | --- |
| GET | `/api/alunos` | 200: lista ordenada por nome e RA |
| POST | `/api/alunos` | 201: aluno criado |
| PUT | `/api/alunos/{id}` | 200: aluno atualizado |
| DELETE | `/api/alunos/{id}` | 204: aluno removido |

Exemplo de corpo para criação e edição:

```json
{
  "ra": "001234",
  "nome": "Ana Silva",
  "dataNascimento": "2000-05-20",
  "dataEntrada": "2026-02-01",
  "curso": "EngenhariaDaComputacao"
}
```

Valores do enum na API: `EngenhariaDaComputacao`, `Administracao` e `EngenhariaDeAlimentos`. Valores numéricos não são aceitos. Datas usam `AAAA-MM-DD`. Violações de negócio retornam 400; RA duplicado, 409; aluno inexistente, 404. Erros de domínio incluem a propriedade `erro`; falhas de leitura do JSON retornam 400 e a interface apresenta uma mensagem genérica.

Este é um projeto educacional para execução local, sem autenticação ou autorização. Utilize dados fictícios.
