using Escola.Aplicacao;
using Escola.Dominio;
using Escola.Infraestrutura;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Curso>(allowIntegerValues: false)));

builder.Services.AddSingleton<IAlunoRepositorio>(_ => new AlunoRepositorioSqlite(
    builder.Configuration["DatabasePath"] ?? Path.Combine(builder.Environment.ContentRootPath, "dados", "escola.db")));

builder.Services.AddTransient<ListarAlunos>();
builder.Services.AddTransient<CriarAluno>();
builder.Services.AddTransient<EditarAluno>();
builder.Services.AddTransient<DeletarAluno>();

var app = builder.Build();

app.Use(async (contexto, proximo) =>
{
    try
    {
        await proximo(contexto);
    }
    catch (Exception erro) when (erro is RegraDeNegocioException or RaDuplicadoException or AlunoNaoEncontradoException)
    {
        contexto.Response.StatusCode = erro switch
        {
            RaDuplicadoException => 409,
            AlunoNaoEncontradoException => 404,
            _ => 400
        };
        await contexto.Response.WriteAsJsonAsync(new { erro = erro.Message });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/alunos", (ListarAlunos caso) => Results.Ok(caso.Executar()));
app.MapPost("/api/alunos", (DadosAluno dados, CriarAluno caso) =>
    Results.Json(caso.Executar(dados), statusCode: 201));

app.MapPut("/api/alunos/{id:guid}", (Guid id, DadosAluno dados, EditarAluno caso) => Results.Ok(caso.Executar(id, dados)));
app.MapDelete("/api/alunos/{id:guid}", (Guid id, DeletarAluno caso) =>
{
    caso.Executar(id);
    return Results.NoContent();
});

app.Run();

public partial class Program;
