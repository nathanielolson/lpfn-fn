using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FunctionApp.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.Services.AddTransient<IFtpToBlobProvider, FtpToBlobService>();
    })
    .Build();

host.Run();

