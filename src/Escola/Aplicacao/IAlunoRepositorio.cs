using Escola.Dominio;

namespace Escola.Aplicacao;

public interface IAlunoRepositorio
{
    IReadOnlyList<Aluno> Listar();
    Aluno? Obter(Guid id);
    void Criar(Aluno aluno);
    void Atualizar(Aluno aluno);
    void Deletar(Guid id);
}

public sealed record DadosAluno(string Ra, string Nome, DateOnly DataNascimento, DateOnly DataEntrada, Curso? Curso)
{
    public Aluno ParaAluno(Guid id) => new(id, Ra, Nome, DataNascimento, DataEntrada,
        Curso ?? throw new RegraDeNegocioException("Selecione um curso."));
}
