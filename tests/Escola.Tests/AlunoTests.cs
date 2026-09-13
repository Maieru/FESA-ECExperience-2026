using Escola.Dominio;

namespace Escola.Tests;

public class AlunoTests
{
    [Fact]
    public void PreservaZerosDoRaENormalizaEspacosDoNome()
    {
        var aluno = (Exemplo.Dados with { Nome = "  Ana   Silva  " }).ParaAluno(Guid.NewGuid());
        Assert.Equal("00123", aluno.Ra);
        Assert.Equal("Ana Silva", aluno.Nome);
    }

    [Theory]
    [InlineData("")][InlineData("12a")][InlineData("-1")][InlineData("1.2")]
    [InlineData("１２")][InlineData("12\n")][InlineData(" 12")][InlineData(null)]
    public void RejeitaRaQueNaoContemSomenteDigitos(string? ra) =>
        Assert.Throws<RegraDeNegocioException>(() => (Exemplo.Dados with { Ra = ra! }).ParaAluno(Guid.NewGuid()));

    [Theory]
    [InlineData("")][InlineData("Ana")][InlineData("Ana 123")][InlineData("   ")][InlineData(null)]
    public void ExigeNomeESobrenome(string? nome) =>
        Assert.Throws<RegraDeNegocioException>(() => (Exemplo.Dados with { Nome = nome! }).ParaAluno(Guid.NewGuid()));

    [Fact]
    public void AceitaCemCaracteresMasRejeitaCentoEUm()
    {
        var nome = "Ana " + new string('a', 96);
        Assert.Equal(100, (Exemplo.Dados with { Nome = nome }).ParaAluno(Guid.NewGuid()).Nome.Length);
        Assert.Throws<RegraDeNegocioException>(() => (Exemplo.Dados with { Nome = nome + "a" }).ParaAluno(Guid.NewGuid()));
    }

    [Fact]
    public void NascimentoDeveSerPosteriorAoAno1950()
    {
        Assert.Throws<RegraDeNegocioException>(() => (Exemplo.Dados with { DataNascimento = new(1950, 12, 31) }).ParaAluno(Guid.NewGuid()));
        Assert.Equal(new(1951, 1, 1), (Exemplo.Dados with { DataNascimento = new(1951, 1, 1) }).ParaAluno(Guid.NewGuid()).DataNascimento);
    }

    [Fact]
    public void ExigeDataDeEntrada() => Assert.Throws<RegraDeNegocioException>(() =>
        (Exemplo.Dados with { DataEntrada = default }).ParaAluno(Guid.NewGuid()));

    [Theory]
    [InlineData(Curso.EngenhariaDaComputacao)][InlineData(Curso.Administracao)][InlineData(Curso.EngenhariaDeAlimentos)]
    public void AceitaCadaCursoDoEnum(Curso curso) =>
        Assert.Equal(curso, (Exemplo.Dados with { Curso = curso }).ParaAluno(Guid.NewGuid()).Curso);

    [Theory]
    [InlineData(null)][InlineData((Curso)99)]
    public void RejeitaCursoAusenteOuInvalido(Curso? curso) => Assert.Throws<RegraDeNegocioException>(() =>
        (Exemplo.Dados with { Curso = curso }).ParaAluno(Guid.NewGuid()));
}
