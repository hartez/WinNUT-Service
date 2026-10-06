using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using NUTDotNetClient;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using WinNUT.ClientService;
using WinNUT.gPRC;
using WinNUT.gPRC.Services;

var builder = WebApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json").Build();

builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "WinNUT UPS Monitor Service";
});

LoggerProviderOptions.RegisterProviderOptions<
	EventLogSettings, EventLogLoggerProvider>(builder.Services);

var nutClientConfigSection = config.GetSection(nameof(NUTClientConfig));
var nutClientConfig = nutClientConfigSection.Get<NUTClientConfig>() ?? new NUTClientConfig();
builder.Services.AddSingleton(nutClientConfig);

var monitorConfigSection = config.GetSection(nameof(MonitorConfig));
var monitorConfig = monitorConfigSection.Get<MonitorConfig>() ?? new MonitorConfig();
builder.Services.AddSingleton(monitorConfig);

// The UPSMonitor will need an IClientGUIService (becuase we're using that for testing)
// But we'll need the concrete implementation for MapGrpcService later on
builder.Services.AddSingleton<ClientGUIService>();
builder.Services.AddSingleton<IClientGUIService>(x => x.GetRequiredService<ClientGUIService>());

builder.Services.AddSingleton<IShutdownService, ShutdownService>();

builder.Services.AddSingleton((services) =>
{
	var config = services.GetRequiredService<NUTClientConfig>();
	return new NUTClient(config.Host, config.Port);
});

builder.Services.AddSingleton<UPSMonitor>();

builder.Services.AddHostedService<NUTClientWorker>();

builder.Services.AddGrpc();

builder.WebHost.UseNamedPipes(options =>
{
	using var serverIdentity = WindowsIdentity.GetCurrent();
	var pipeSecurity = new PipeSecurity();
	
	pipeSecurity.AddAccessRule(new PipeAccessRule(
		serverIdentity.User!,
		PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
		AccessControlType.Allow));

	pipeSecurity.AddAccessRule(new PipeAccessRule(
		"Everyone",
		PipeAccessRights.ReadWrite,
		AccessControlType.Allow));

	options.PipeSecurity = pipeSecurity;
	options.CurrentUserOnly = false;
});

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
