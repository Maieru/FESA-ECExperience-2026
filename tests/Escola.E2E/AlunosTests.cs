using Xunit.Abstractions;

namespace Escola.E2E;

[Trait("Categoria", "E2E")]
public class AlunosTests(ITestOutputHelper output) : CenarioE2E(output)
{
    private sealed record Cadastro(
        string Ra = "001234", string Nome = "Ana Silva", string Nascimento = "2000-05-20",
        string Entrada = "2026-02-01", string Curso = "Engenharia da Computação");

    private ILocator Botao(string nome) => Pagina.GetByRole(AriaRole.Button, new() { Name = nome, Exact = true });
    private ILocator Campo(string nome) => Pagina.GetByLabel(nome, new() { Exact = true });
    private ILocator Mensagem => Pagina.GetByRole(AriaRole.Status);
    private ILocator Linha(string ra = "001234") => Pagina.GetByRole(AriaRole.Row).Filter(new()
    {
        Has = Pagina.GetByText($"RA {ra}", new() { Exact = true })
    });

    private async Task Preencher(Cadastro? dados = null)
    {
        dados ??= new();
        await Campo("RA").FillAsync(dados.Ra);
        await Campo("Nome completo").FillAsync(dados.Nome);
        await Campo("Data de nascimento").FillAsync(dados.Nascimento);
        await Campo("Entrada na escola").FillAsync(dados.Entrada);
        await Campo("Curso").SelectOptionAsync(new SelectOptionValue { Label = dados.Curso });
    }

    private async Task Cadastrar(Cadastro? dados = null)
    {
        dados ??= new();
        await Preencher(dados);
        await Botao("Cadastrar aluno").ClickAsync();
        await Expect(Linha(dados.Ra)).ToBeVisibleAsync();
        await Expect(Botao("Cadastrar aluno")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task MostraEstadoVazioETresCursosDisponiveis()
    {
        await Expect(Pagina.GetByRole(AriaRole.Heading, new() { Name = "Alunos cadastrados" })).ToBeVisibleAsync();
        await Expect(Pagina.GetByRole(AriaRole.Table)).ToBeHiddenAsync();
        await Expect(Campo("Curso").Locator("option")).ToHaveTextAsync(new[]
        {
            "Selecione um curso", "Engenharia da Computação", "Administração", "Engenharia de Alimentos"
        });
    }

    [Fact]
    public async Task CadastraEListaTodosOsCamposPreservandoZerosAposRecarregar()
    {
        await Cadastrar();
        await Expect(Mensagem).ToHaveTextAsync("Aluno cadastrado.");
        await Expect(Campo("Nome completo")).ToBeEmptyAsync();
        await Pagina.ReloadAsync();
        foreach (var texto in new[] { "Ana Silva", "RA 001234", "Engenharia da Computação", "20/05/2000", "01/02/2026" })
            await Expect(Linha()).ToContainTextAsync(texto);
    }

    [Fact]
    public async Task EditaTodosOsCamposSemCriarOutroAluno()
    {
        await Cadastrar();
        await Botao("Editar Ana Silva").ClickAsync();
        await Expect(Campo("RA")).ToHaveValueAsync("001234");
        await Preencher(new("009999", "José Santos", "1999-03-04", "2025-01-02", "Administração"));
        await Botao("Salvar alterações").ClickAsync();
        await Expect(Mensagem).ToHaveTextAsync("Aluno atualizado.");
        await Expect(Linha("009999")).ToBeVisibleAsync();
        await Pagina.ReloadAsync();
        await Expect(Pagina.GetByRole(AriaRole.Row)).ToHaveCountAsync(2); // Cabeçalho e um aluno.
        foreach (var texto in new[] { "José Santos", "Administração", "04/03/1999", "02/01/2025" })
            await Expect(Linha("009999")).ToContainTextAsync(texto);
        await Expect(Linha()).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task CancelarEdicaoDescartaAlteracoes()
    {
        await Cadastrar();
        await Botao("Editar Ana Silva").ClickAsync();
        await Campo("Nome completo").FillAsync("Nome Alterado");
        await Botao("Cancelar").ClickAsync();
        await Expect(Pagina.GetByRole(AriaRole.Heading, new() { Name = "Novo aluno" })).ToBeVisibleAsync();
        await Expect(Campo("Nome completo")).ToBeEmptyAsync();
        await Pagina.ReloadAsync();
        await Expect(Linha()).ToContainTextAsync("Ana Silva");
    }

    [Fact]
    public async Task CancelarExclusaoMantemAlunoEConfirmarExcluiSomenteOEscolhido()
    {
        await Cadastrar();
        await Cadastrar(new(Ra: "002345", Nome: "Bruno Souza", Curso: "Engenharia de Alimentos"));
        await ResponderExclusao(aceitar: false);
        await Expect(Linha()).ToBeVisibleAsync();
        await Pagina.ReloadAsync();
        await Expect(Linha()).ToBeVisibleAsync();
        await ResponderExclusao(aceitar: true);
        await Expect(Mensagem).ToHaveTextAsync("Aluno deletado.");
        await Expect(Linha()).ToHaveCountAsync(0);
        await Pagina.ReloadAsync();
        await Expect(Linha("002345")).ToContainTextAsync("Bruno Souza");
        await Expect(Linha()).ToHaveCountAsync(0);
    }

    private async Task ResponderExclusao(bool aceitar)
    {
        var resposta = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        async void AoAbrirDialogo(object? sender, IDialog dialogo)
        {
            try
            {
                if (aceitar) await dialogo.AcceptAsync();
                else await dialogo.DismissAsync();
                resposta.SetResult(dialogo.Message);
            }
            catch (Exception erro) { resposta.SetException(erro); }
        }
        Pagina.Dialog += AoAbrirDialogo;
        try
        {
            await Botao("Deletar Ana Silva").ClickAsync();
            Assert.Equal("Deletar Ana Silva (RA 001234)?", await resposta.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally { Pagina.Dialog -= AoAbrirDialogo; }
    }

    [Fact]
    public async Task RaDuplicadoMostraErroEPermiteCorrigirCadastro()
    {
        await Cadastrar();
        await Preencher(new(Nome: "Bruno Souza"));
        await Botao("Cadastrar aluno").ClickAsync();
        await Expect(Mensagem).ToHaveTextAsync("Já existe um aluno com esse RA.");
        await Expect(Campo("Nome completo")).ToHaveValueAsync("Bruno Souza");
        await Expect(Pagina.GetByRole(AriaRole.Row)).ToHaveCountAsync(2);
        await Campo("RA").FillAsync("002345");
        await Botao("Cadastrar aluno").ClickAsync();
        await Expect(Linha("002345")).ToContainTextAsync("Bruno Souza");
    }

    [Fact]
    public async Task EdicaoSemSobrenomeMostraValidacaoEPreservaCadastro()
    {
        await Cadastrar();
        await Botao("Editar Ana Silva").ClickAsync();
        await Campo("Nome completo").FillAsync("Ana");
        await Botao("Salvar alterações").ClickAsync();
        await Expect(Mensagem).ToHaveTextAsync("Informe nome e sobrenome, com até 100 caracteres.");
        await Expect(Campo("Nome completo")).ToHaveValueAsync("Ana");
        await Pagina.ReloadAsync();
        await Expect(Linha()).ToContainTextAsync("Ana Silva");
    }

    [Theory]
    [InlineData("RA", "12abc")]
    [InlineData("Data de nascimento", "1950-12-31")]
    [InlineData("Entrada na escola", "")]
    public async Task ValidacaoDoNavegadorImpedeCadastroInvalido(string nomeCampo, string valor)
    {
        await Preencher();
        var campo = Campo(nomeCampo);
        await campo.FillAsync(valor);
        await Botao("Cadastrar aluno").ClickAsync();
        await Expect(campo).ToBeFocusedAsync();
        Assert.False(await campo.EvaluateAsync<bool>("elemento => elemento.validity.valid"));
        await Pagina.ReloadAsync();
        await Expect(Pagina.GetByText("Nenhum aluno cadastrado.", new() { Exact = false })).ToBeVisibleAsync();
    }
}
