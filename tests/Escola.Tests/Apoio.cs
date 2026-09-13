using Escola.Aplicacao;
using Escola.Dominio;

namespace Escola.Tests;

internal static class Exemplo
{
    public static DadosAluno Dados => new("00123", "Ana Silva", new(2000, 5, 20), new(2026, 2, 1), Curso.EngenhariaDaComputacao);
}

// Implementação substituta: testes unitários não precisam de HTTP nem banco.
internal sealed class RepositorioEmMemoria : IAlunoRepositorio
{
    private readonly Dictionary<Guid, Aluno> alunos = [];
    public IReadOnlyList<Aluno> Listar() => alunos.Values.OrderBy(a => a.Nome).ToList();
    public Aluno? Obter(Guid id) => alunos.GetValueOrDefault(id);
    public void Criar(Aluno aluno)
    {
        ValidarRa(aluno);
        alunos.Add(aluno.Id, aluno);
    }
    public void Atualizar(Aluno aluno)
    {
        if (!alunos.ContainsKey(aluno.Id)) throw new AlunoNaoEncontradoException();
        ValidarRa(aluno);
        alunos[aluno.Id] = aluno;
    }
    private void ValidarRa(Aluno aluno)
    {
        if (alunos.Values.Any(a => a.Ra == aluno.Ra && a.Id != aluno.Id)) throw new RaDuplicadoException();
    }
    public void Deletar(Guid id)
    {
        if (!alunos.Remove(id)) throw new AlunoNaoEncontradoException();
    }
}
