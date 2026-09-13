# FESA · EC Experience 2026

Webapp didático criado para o evento **EC Experience 2026 da faculdade FESA**, como exemplo para a apresentação **“Qualidade de Software nos Dias Atuais — Do código ao CI/CD”**. O objetivo é mostrar, com um cadastro pequeno de alunos, como separar regras de negócio, persistência e interface para mudar o código com segurança.

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

Todos os campos podem ser editados. Um `Id` interno mantém a identidade do aluno quando o RA muda. RAs como `00123` e `123` são identificadores distintos. Não foram adicionadas regras de idade máxima, proibição de datas futuras ou relação entre nascimento e entrada: são possíveis exercícios de evolução do domínio.

## Arquitetura simples e testável

```text
src/Escola/
  Dominio/Aluno.cs                   Entidade, enum e validações
  Aplicacao/IAlunoRepositorio.cs     Contrato da persistência e dados de entrada
  Aplicacao/CriarAluno.cs            Caso de uso de criação
  Aplicacao/ListarAlunos.cs          Caso de uso de listagem
  Aplicacao/EditarAluno.cs           Caso de uso de edição
  Aplicacao/DeletarAluno.cs          Caso de uso de exclusão
  Infraestrutura/AlunoRepositorioSqlite.cs
  Program.cs                        Rotas HTTP e injeção de dependências
  wwwroot/                          Interface HTML, CSS e JavaScript
tests/Escola.Tests/
  AlunoTests.cs                     Regras e limites do domínio
  CasosDeUsoTests.cs                Quatro operações com repositório em memória
  ApiTests.cs                       HTTP + casos de uso + SQLite real
```

A classe `Aluno` é imutável e impede a construção de dados inválidos. Os quatro casos de uso recebem `IAlunoRepositorio` pelo construtor. O repositório SQLite implementa esse contrato usando SQL parametrizado; a restrição `UNIQUE` garante a unicidade do RA também no banco. A edição constrói e valida uma nova versão antes de persistir.

Um único projeto de aplicação, organizado por pastas, mantém a navegação fácil durante a palestra. A interface usa JavaScript sem framework, e o servidor usa ASP.NET Core Minimal APIs.

## Testes

```sh
dotnet test
```

Executar apenas um nível:

```sh
dotnet test --filter "FullyQualifiedName!~ApiTests"
dotnet test --filter "FullyQualifiedName~ApiTests"
```

- **Unitários:** regras de RA, nome, datas e enum; criação, listagem, edição e exclusão; duplicidade, inexistência e preservação dos dados após validação rejeitada.
- **Integração:** requisições HTTP com `WebApplicationFactory`, serialização JSON, status 200/201/204/400/404/409, quatro operações com SQLite real, leitura do arquivo por outra instância, campos ausentes, datas impossíveis, enum inválido e arquivos da interface.

Cada teste de integração cria seu próprio banco em uma pasta temporária e o remove ao terminar. O banco usado na demonstração não é alterado. Os testes automatizados de integração não controlam o navegador.

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

## Roteiro breve para a apresentação

1. Cadastre um aluno, edite seu curso e confira a listagem.
2. Tente cadastrar o mesmo RA e um nome sem sobrenome para demonstrar as regras.
3. Mostre a entidade e um teste de limite, como 31/12/1950 versus 01/01/1951.
4. Compare um teste unitário com um teste que atravessa HTTP e SQLite.
5. Altere uma regra e execute os testes para discutir regressões e refatoração.
6. Exclua um aluno e reinicie o app para demonstrar persistência.

Este é um exemplo educacional para execução local, sem autenticação ou autorização. Não é um sistema acadêmico pronto para produção; use dados fictícios na demonstração.
