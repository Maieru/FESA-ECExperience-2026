using Escola.Dominio;

namespace Escola.Aplicacao;

public sealed class CriarAluno(IAlunoRepositorio repositorio)
{
    public Aluno Executar(DadosAluno dados)
    {
        var aluno = dados.ParaAluno(Guid.NewGuid());
        repositorio.Criar(aluno);
        return aluno;
    }
}
