using LibreHardwareService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => { options.ServiceName = builder.Environment.ApplicationName; });

ConfigHelper.Config.initialize(builder.Configuration);
builder.Services.AddHostedService<Service>();

var host = builder.Build();
host.Run();
