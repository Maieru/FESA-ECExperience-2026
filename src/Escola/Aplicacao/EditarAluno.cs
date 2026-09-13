using Escola.Dominio;

namespace Escola.Aplicacao;

public sealed class EditarAluno(IAlunoRepositorio repositorio)
{
    public Aluno Executar(Guid id, DadosAluno dados)
    {
        _ = repositorio.Obter(id) ?? throw new AlunoNaoEncontradoException();
        var aluno = dados.ParaAluno(id);
        repositorio.Atualizar(aluno);
        return aluno;
    }
}
