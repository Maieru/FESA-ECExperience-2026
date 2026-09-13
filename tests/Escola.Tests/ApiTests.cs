using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Escola.Aplicacao;
using Escola.Dominio;
using Escola.Infraestrutura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Escola.Tests;

// Cada teste usa HTTP em memória e um arquivo SQLite real, exclusivo e temporário.
public sealed class ApiTests : IDisposable
{
    private readonly string pasta = Path.Combine(Path.GetTempPath(), "fesa-tests", Guid.NewGuid().ToString());
    private readonly WebApplicationFactory<Program> fabrica;
    private readonly HttpClient cliente;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<Curso>() }
    };
    private string Banco => Path.Combine(pasta, "teste.db");

    public ApiTests()
    {
        fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing").ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["DatabasePath"] = Banco })));
        cliente = fabrica.CreateClient();
    }

    private async Task<Aluno> Criar(DadosAluno? dados = null)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/alunos", dados ?? Exemplo.Dados, Json);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<Aluno>(Json))!;
    }

    private async Task<List<Aluno>> Listar() => (await cliente.GetFromJsonAsync<List<Aluno>>("/api/alunos", Json))!;

    [Fact]
    public async Task ListaVaziaRetorna200() => Assert.Empty(await Listar());

    [Fact]
    public async Task CriacaoPersisteTodosOsCamposEmArquivo()
    {
        var criado = await Criar();
        var listado = Assert.Single(await Listar());
        Assert.Equal(criado.Id, listado.Id);
        Assert.Equal(Exemplo.Dados, new DadosAluno(listado.Ra, listado.Nome, listado.DataNascimento, listado.DataEntrada, listado.Curso));
        // Outra instância lê o arquivo, sem depender do estado da aplicação HTTP.
        Assert.Equal(criado.Id, Assert.Single(new AlunoRepositorioSqlite(Banco).Listar()).Id);
    }

    [Fact]
    public async Task ListaMultiplosAlunosPorNome()
    {
        await Criar(Exemplo.Dados with { Ra = "2", Nome = "Zélia Souza" });
        await Criar();
        Assert.Equal(new[] { "Ana Silva", "Zélia Souza" }, (await Listar()).Select(a => a.Nome));
    }

    [Fact]
    public async Task EditaTodosOsCamposEPersiste()
    {
        var aluno = await Criar();
        var dados = new DadosAluno("987", "José Santos", new(1998, 2, 3), new(2024, 4, 5), Curso.EngenhariaDeAlimentos);
        var resposta = await cliente.PutAsJsonAsync($"/api/alunos/{aluno.Id}", dados, Json);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var salvo = Assert.Single(new AlunoRepositorioSqlite(Banco).Listar());
        Assert.Equal(aluno.Id, salvo.Id);
        Assert.Equal(dados, new DadosAluno(salvo.Ra, salvo.Nome, salvo.DataNascimento, salvo.DataEntrada, salvo.Curso));
    }

    [Fact]
    public async Task EditaMantendoOProprioRa()
    {
        var aluno = await Criar();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/alunos/{aluno.Id}", Exemplo.Dados with { Nome = "Ana Santos" }, Json)).StatusCode);
        Assert.Equal("Ana Santos", Assert.Single(await Listar()).Nome);
    }

    [Fact]
    public async Task DeletaSomenteOAlunoEscolhidoEPersiste()
    {
        var aluno = await Criar();
        var outro = await Criar(Exemplo.Dados with { Ra = "2" });
        var resposta = await cliente.DeleteAsync($"/api/alunos/{aluno.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal("", await resposta.Content.ReadAsStringAsync());
        Assert.Equal(outro.Id, Assert.Single(await Listar()).Id);
        Assert.Null(new AlunoRepositorioSqlite(Banco).Obter(aluno.Id));
    }

    [Theory]
    [InlineData("PUT")][InlineData("DELETE")]
    public async Task AlunoInexistenteRetorna404(string metodo)
    {
        var request = new HttpRequestMessage(new HttpMethod(metodo), $"/api/alunos/{Guid.NewGuid()}");
        if (metodo == "PUT") request.Content = JsonContent.Create(Exemplo.Dados, options: Json);
        var resposta = await cliente.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Contains("Aluno não encontrado", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CriarRaDuplicadoRetorna409SemDuplicar()
    {
        await Criar();
        var resposta = await cliente.PostAsJsonAsync("/api/alunos", Exemplo.Dados, Json);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Single(await Listar());
    }

    [Fact]
    public async Task EditarComRaDuplicadoRetorna409EPreservaDados()
    {
        await Criar();
        var outro = await Criar(Exemplo.Dados with { Ra = "2" });
        var resposta = await cliente.PutAsJsonAsync($"/api/alunos/{outro.Id}", Exemplo.Dados, Json);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("2", (await Listar()).Single(a => a.Id == outro.Id).Ra);
    }

    public static IEnumerable<object[]> DadosInvalidos()
    {
        var valido = JsonSerializer.SerializeToNode(Exemplo.Dados, Json)!.AsObject();
        foreach (var (campo, valor) in new (string, object?)[] {
            ("ra", "12A"), ("ra", null), ("ra", 123), ("nome", "Ana"), ("nome", new string('a', 101) + " Silva"),
            ("dataNascimento", "1950-12-31"), ("dataNascimento", "2001-02-29"), ("dataEntrada", "data"),
            ("curso", "Medicina"), ("curso", 0), ("curso", 99), ("curso", null) })
        {
            var copia = valido.DeepClone().AsObject();
            copia[campo] = JsonSerializer.SerializeToNode(valor);
            yield return [copia.ToJsonString()];
        }
        foreach (var campo in new[] { "ra", "nome", "dataNascimento", "dataEntrada", "curso" })
        {
            var copia = valido.DeepClone().AsObject();
            copia.Remove(campo);
            yield return [copia.ToJsonString()];
        }
        yield return ["{json inválido"];
    }

    [Theory]
    [MemberData(nameof(DadosInvalidos))]
    public async Task CriacaoEEdicaoInvalidasRetornam400SemAlterarBanco(string json)
    {
        var criado = await Criar();
        foreach (var metodo in new[] { "POST", "PUT" })
        {
            var request = new HttpRequestMessage(new HttpMethod(metodo), metodo == "POST" ? "/api/alunos" : $"/api/alunos/{criado.Id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.SendAsync(request)).StatusCode);
            var aluno = Assert.Single(await Listar());
            Assert.Equal(Exemplo.Dados, new DadosAluno(aluno.Ra, aluno.Nome, aluno.DataNascimento, aluno.DataEntrada, aluno.Curso));
        }
    }

    [Fact]
    public async Task ServeInterfaceEArquivosEstaticos()
    {
        Assert.Contains("Gestão de alunos", await cliente.GetStringAsync("/"));
        Assert.NotEmpty(await cliente.GetStringAsync("/app.js"));
        Assert.NotEmpty(await cliente.GetStringAsync("/style.css"));
    }

    public void Dispose()
    {
        cliente.Dispose();
        fabrica.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true);
    }
}
