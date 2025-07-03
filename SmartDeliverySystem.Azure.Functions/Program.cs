using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Додаємо HttpClient для викликів API
        services.AddHttpClient();
    })
    .Build();

host.Run();
