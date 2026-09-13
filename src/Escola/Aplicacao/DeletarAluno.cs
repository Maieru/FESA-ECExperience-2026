namespace Escola.Aplicacao;

public sealed class DeletarAluno(IAlunoRepositorio repositorio)
{
    public void Executar(Guid id) => repositorio.Deletar(id);
}
