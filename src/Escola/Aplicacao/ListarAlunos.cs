using Escola.Dominio;

namespace Escola.Aplicacao;

public sealed class ListarAlunos(IAlunoRepositorio repositorio)
{
    public IReadOnlyList<Aluno> Executar() => repositorio.Listar();
}
