using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WinNUT_ClientService;

var builder = Host.CreateApplicationBuilder(args);

var config = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json").Build();

var section = config.GetSection(nameof(NUTClientConfig));
var nutClientConfig	= section.Get<NUTClientConfig>() ?? new NUTClientConfig("localhost");

builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "NUT Client Service";
});

LoggerProviderOptions.RegisterProviderOptions<
	EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton(nutClientConfig);
builder.Services.AddHostedService<NUTClientWorker>();

var host = builder.Build();
host.Run();
