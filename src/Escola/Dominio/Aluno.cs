using System.Text.RegularExpressions;

namespace Escola.Dominio;

public enum Curso { EngenhariaDaComputacao, Administracao, EngenhariaDeAlimentos }

public sealed class RegraDeNegocioException(string mensagem) : Exception(mensagem);
public sealed class AlunoNaoEncontradoException() : Exception("Aluno não encontrado.");
public sealed class RaDuplicadoException() : Exception("Já existe um aluno com esse RA.");

public sealed class Aluno
{
    private const int TamanhoMaximoNome = 100;
    private const int AnoMinimoDataNascimento = 1950;
    private const int TimeoutRegexMilliseconds = 50;
    private const int NumeroMinimoPartesParaNomeSobrenome = 2;
    private const string PadraoSomenteNumeros = @"\A[0-9]+\z";
    private const string PadraoEspacos = @"\s+";

    public Guid Id { get; }
    public string Ra { get; }
    public string Nome { get; }
    public DateOnly DataNascimento { get; }
    public DateOnly DataEntrada { get; }
    public Curso Curso { get; }

    //Utilizar como base na refatoração, remover antes do PR 
    //
    //public Aluno(Guid id, string ra, string nome, DateOnly dataNascimento, DateOnly dataEntrada, Curso curso)
    //{
    //    if (string.IsNullOrEmpty(ra) || !Regex.IsMatch(ra, @"\A[0-9]+\z", RegexOptions.None, TimeSpan.FromMilliseconds(50)))
    //        throw new RegraDeNegocioException("O RA deve conter somente dígitos de 0 a 9.");

    //    nome = Regex.Replace(nome?.Trim() ?? "", @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(50));

    //    if (nome.Length > TamanhoMaximoNome || nome.Split(' ').Count(parte => parte.Any(char.IsLetter)) < 2)
    //        throw new RegraDeNegocioException("Informe nome e sobrenome, com até 100 caracteres.");

    //    if (dataNascimento.Year <= AnoMinimoDataNascimento)
    //        throw new RegraDeNegocioException("O nascimento deve ser a partir de 01/01/1951.");

    //    if (dataEntrada == default)
    //        throw new RegraDeNegocioException("Informe a data de entrada na escola.");

    //    if (!Enum.IsDefined(curso))
    //        throw new RegraDeNegocioException("Selecione um curso válido.");

    //    Id = id;
    //    Ra = ra;
    //    Nome = nome;
    //    DataNascimento = dataNascimento;
    //    DataEntrada = dataEntrada;
    //    Curso = curso;
    //}

    public Aluno(Guid id, string ra, string nome, DateOnly dataNascimento, DateOnly dataEntrada, Curso curso)
    {
        if (string.IsNullOrEmpty(ra) || !Regex.IsMatch(ra, PadraoSomenteNumeros, RegexOptions.None, TimeSpan.FromMilliseconds(TimeoutRegexMilliseconds)))
            throw new RegraDeNegocioException("O RA deve conter somente dígitos de 0 a 9.");

        nome = NormalizaNome(nome);

        if (nome.Length > TamanhoMaximoNome || nome.Split(' ').Count(parte => parte.Any(char.IsLetter)) < NumeroMinimoPartesParaNomeSobrenome)
            throw new RegraDeNegocioException("Informe nome e sobrenome, com até 100 caracteres.");

        if (dataNascimento.Year <= AnoMinimoDataNascimento)
            throw new RegraDeNegocioException("O nascimento deve ser a partir de 01/01/1951.");

        if (dataEntrada == default)
            throw new RegraDeNegocioException("Informe a data de entrada na escola.");

        if (!Enum.IsDefined(curso))
            throw new RegraDeNegocioException("Selecione um curso válido.");

        Id = id;
        Ra = ra;
        Nome = nome;
        DataNascimento = dataNascimento;
        DataEntrada = dataEntrada;
        Curso = curso;
    }

    private static string NormalizaNome(string nome) => Regex.Replace(nome?.Trim() ?? "", PadraoEspacos, " ", RegexOptions.None, TimeSpan.FromMilliseconds(TimeoutRegexMilliseconds));
}
