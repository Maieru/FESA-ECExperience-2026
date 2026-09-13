using Escola.Aplicacao;
using Escola.Dominio;
using Microsoft.Data.Sqlite;

namespace Escola.Infraestrutura;

public sealed class AlunoRepositorioSqlite : IAlunoRepositorio
{
    private readonly string connectionString;

    public AlunoRepositorioSqlite(string caminho)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = caminho }.ToString();
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            CREATE TABLE IF NOT EXISTS Alunos (
                Id TEXT PRIMARY KEY, Ra TEXT NOT NULL UNIQUE, Nome TEXT NOT NULL,
                DataNascimento TEXT NOT NULL, DataEntrada TEXT NOT NULL, Curso INTEGER NOT NULL
            );
            """;
        _ = comando.ExecuteNonQuery();
    }

    private SqliteConnection Abrir()
    {
        var conexao = new SqliteConnection(connectionString);
        conexao.Open();
        return conexao;
    }

    private static Aluno Ler(SqliteDataReader leitor) => new(
        Guid.Parse(leitor.GetString(0)), leitor.GetString(1), leitor.GetString(2),
        DateOnly.ParseExact(leitor.GetString(3), "yyyy-MM-dd"),
        DateOnly.ParseExact(leitor.GetString(4), "yyyy-MM-dd"), (Curso)leitor.GetInt32(5));

    public IReadOnlyList<Aluno> Listar()
    {
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Ra, Nome, DataNascimento, DataEntrada, Curso FROM Alunos ORDER BY Nome, Ra";
        using var leitor = comando.ExecuteReader();
        var alunos = new List<Aluno>();
        while (leitor.Read())
            alunos.Add(Ler(leitor));
        return alunos;
    }

    public Aluno? Obter(Guid id)
    {
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Ra, Nome, DataNascimento, DataEntrada, Curso FROM Alunos WHERE Id = $id";
        _ = comando.Parameters.AddWithValue("$id", id.ToString());
        using var leitor = comando.ExecuteReader();
        return leitor.Read() ? Ler(leitor) : null;
    }

    public void Criar(Aluno aluno) => Salvar(aluno, """
        INSERT INTO Alunos (Id, Ra, Nome, DataNascimento, DataEntrada, Curso)
        VALUES ($id, $ra, $nome, $nascimento, $entrada, $curso)
        """);

    public void Atualizar(Aluno aluno) => Salvar(aluno, """
        UPDATE Alunos SET Ra = $ra, Nome = $nome, DataNascimento = $nascimento,
        DataEntrada = $entrada, Curso = $curso WHERE Id = $id
        """);

    private void Salvar(Aluno aluno, string sql)
    {
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        _ = comando.Parameters.AddWithValue("$id", aluno.Id.ToString());
        _ = comando.Parameters.AddWithValue("$ra", aluno.Ra);
        _ = comando.Parameters.AddWithValue("$nome", aluno.Nome);
        _ = comando.Parameters.AddWithValue("$nascimento", aluno.DataNascimento.ToString("yyyy-MM-dd"));
        _ = comando.Parameters.AddWithValue("$entrada", aluno.DataEntrada.ToString("yyyy-MM-dd"));
        _ = comando.Parameters.AddWithValue("$curso", (int)aluno.Curso);
        try
        {
            if (comando.ExecuteNonQuery() == 0)
                throw new AlunoNaoEncontradoException();
        }
        catch (SqliteException erro) when (erro.SqliteExtendedErrorCode == 2067)
        {
            throw new RaDuplicadoException();
        }
    }

    public void Deletar(Guid id)
    {
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM Alunos WHERE Id = $id";
        _ = comando.Parameters.AddWithValue("$id", id.ToString());

        if (comando.ExecuteNonQuery() == 0)
            throw new AlunoNaoEncontradoException();
    }
}
