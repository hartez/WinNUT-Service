using WinNUT.gPRC.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WinNUT.ClientService;
using WinNUT.gPRC;
using NUTDotNetClient;
using System.Security.Cryptography;
using Grpc.Core;

var builder = WebApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json").Build();

builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "NUT Client Service";
});

LoggerProviderOptions.RegisterProviderOptions<
	EventLogSettings, EventLogLoggerProvider>(builder.Services);

var section = config.GetSection(nameof(NUTClientConfig));
var nutClientConfig = section.Get<NUTClientConfig>() ?? new NUTClientConfig("localhost");
builder.Services.AddSingleton(nutClientConfig);

// The UPSMonitor will need an IClientGUIService (becuase we're using that for testing)
// But we'll need the concrete implementation for MapGrpcService later on
builder.Services.AddSingleton<ClientGUIService>();
builder.Services.AddSingleton<IClientGUIService>(x => x.GetRequiredService<ClientGUIService>());

builder.Services.AddSingleton<IShutdownService, ShutdownService>();

builder.Services.AddSingleton((services) => {
	var config = services.GetRequiredService<NUTClientConfig>();
	return new NUTClient(config.Host, config.Port);
});

builder.Services.AddSingleton<UPSMonitor>();

builder.Services.AddHostedService<NUTClientWorker>();

builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.ListenNamedPipe(Values.PipeName);

	serverOptions.ConfigureEndpointDefaults(listenOptions =>
	{
		listenOptions.Protocols = HttpProtocols.Http2;
	});
});

var host = builder.Build();

host.MapGrpcService<ClientGUIService>();
host.Run();
