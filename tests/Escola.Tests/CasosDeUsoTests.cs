using Escola.Aplicacao;
using Escola.Dominio;

namespace Escola.Tests;

public class CasosDeUsoTests
{
    private readonly RepositorioEmMemoria repositorio = new();

    [Fact]
    public void ListaVaziaQuandoNaoHaAlunos() => Assert.Empty(new ListarAlunos(repositorio).Executar());

    [Fact]
    public void CriaAlunoEListaDadosPersistidos()
    {
        var aluno = new CriarAluno(repositorio).Executar(Exemplo.Dados);
        Assert.NotEqual(Guid.Empty, aluno.Id);
        Assert.Same(aluno, Assert.Single(new ListarAlunos(repositorio).Executar()));
    }

    [Fact]
    public void CriacaoInvalidaNaoPersiste()
    {
        Assert.Throws<RegraDeNegocioException>(() => new CriarAluno(repositorio).Executar(Exemplo.Dados with { Nome = "Ana" }));
        Assert.Empty(repositorio.Listar());
    }

    [Fact]
    public void NaoCriaRaDuplicado()
    {
        var criar = new CriarAluno(repositorio);
        criar.Executar(Exemplo.Dados);
        Assert.Throws<RaDuplicadoException>(() => criar.Executar(Exemplo.Dados));
        Assert.Single(repositorio.Listar());
    }

    [Fact]
    public void EditaTodosOsCamposPreservandoIdentidade()
    {
        var original = new CriarAluno(repositorio).Executar(Exemplo.Dados);
        var dados = new DadosAluno("00999", "João Santos", new(1999, 3, 4), new(2025, 1, 2), Curso.Administracao);
        var editado = new EditarAluno(repositorio).Executar(original.Id, dados);
        Assert.Equal(original.Id, editado.Id);
        Assert.Equal(dados, new DadosAluno(editado.Ra, editado.Nome, editado.DataNascimento, editado.DataEntrada, editado.Curso));
        Assert.Same(editado, repositorio.Obter(original.Id));
    }

    [Fact]
    public void EdicaoInvalidaPreservaOriginal()
    {
        var aluno = new CriarAluno(repositorio).Executar(Exemplo.Dados);
        Assert.Throws<RegraDeNegocioException>(() => new EditarAluno(repositorio).Executar(aluno.Id, Exemplo.Dados with { Ra = "abc" }));
        Assert.Same(aluno, repositorio.Obter(aluno.Id));
    }

    [Fact]
    public void EdicaoNaoPodeUsarRaDeOutroAluno()
    {
        var criar = new CriarAluno(repositorio);
        criar.Executar(Exemplo.Dados);
        var outro = criar.Executar(Exemplo.Dados with { Ra = "456" });
        Assert.Throws<RaDuplicadoException>(() => new EditarAluno(repositorio).Executar(outro.Id, Exemplo.Dados));
        Assert.Equal("456", repositorio.Obter(outro.Id)!.Ra);
    }

    [Fact]
    public void EditarInexistenteFalha() => Assert.Throws<AlunoNaoEncontradoException>(() =>
        new EditarAluno(repositorio).Executar(Guid.NewGuid(), Exemplo.Dados));

    [Fact]
    public void DeletaSomenteAlunoEscolhido()
    {
        var criar = new CriarAluno(repositorio);
        var aluno = criar.Executar(Exemplo.Dados);
        var outro = criar.Executar(Exemplo.Dados with { Ra = "456" });
        new DeletarAluno(repositorio).Executar(aluno.Id);
        Assert.Same(outro, Assert.Single(repositorio.Listar()));
    }

    [Fact]
    public void DeletarInexistenteFalha() => Assert.Throws<AlunoNaoEncontradoException>(() =>
        new DeletarAluno(repositorio).Executar(Guid.NewGuid()));
}
