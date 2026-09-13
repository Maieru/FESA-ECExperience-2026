using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using Xunit.Abstractions;

namespace Escola.E2E;

public abstract class CenarioE2E(ITestOutputHelper output) : IAsyncLifetime
{
    private WebApplicationFactory<Program>? fabrica;
    private IPlaywright? playwright;
    private IBrowser? navegador;
    private IBrowserContext? contexto;
    private string pasta = "";
    protected IPage Pagina { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);

        while (raiz is not null && !File.Exists(Path.Combine(raiz.FullName, "FESA-ECExperience-2026.slnx")))
            raiz = raiz.Parent;

        if (raiz is null)
            throw new InvalidOperationException("Raiz do projeto não encontrada.");

        pasta = Path.Combine(raiz.FullName, "artifacts", "e2e", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(pasta);
        output.WriteLine($"Artefatos deste cenário: {pasta}");

        fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0))
                .ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabasePath"] = Path.Combine(pasta, "escola.db")
                })));

        fabrica.UseKestrel(0);
        using var cliente = fabrica.CreateClient();

        playwright = await Playwright.CreateAsync();
        navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
        {
            SlowMo = 30,
            Channel = Environment.GetEnvironmentVariable("E2E_CHANNEL"),
            Headless = Environment.GetEnvironmentVariable("E2E_HEADEDLESS") == "1"
        });
        contexto = await navegador.NewContextAsync(new()
        {
            BaseURL = cliente.BaseAddress!.ToString(),
            Locale = "pt-BR",
            ViewportSize = new() { Width = 1280, Height = 720 }
        });
        await contexto.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        Pagina = await contexto.NewPageAsync();
        _ = await Pagina.GotoAsync("/");
        await Expect(Pagina.GetByText("Nenhum aluno cadastrado.", new() { Exact = false })).ToBeVisibleAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (contexto is not null)
            {
                try
                {
                    if (Pagina is not null && !Pagina.IsClosed)
                        _ = await Pagina.ScreenshotAsync(new() { Path = Path.Combine(pasta, "pagina.png"), FullPage = true });
                }
                catch (PlaywrightException erro) { output.WriteLine($"Screenshot indisponível: {erro.Message}"); }
                finally
                {
                    await contexto.Tracing.StopAsync(new() { Path = Path.Combine(pasta, "trace.zip") });
                }
            }
        }
        finally
        {
            try
            {
                if (navegador is not null)
                    await navegador.CloseAsync();
            }
            finally
            {
                playwright?.Dispose();
                if (fabrica is not null)
                    await fabrica.DisposeAsync();
            }
        }
    }
}
