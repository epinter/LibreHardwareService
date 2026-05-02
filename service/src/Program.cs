using LibreHardwareService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => { options.ServiceName = builder.Environment.ApplicationName; });

ConfigHelper.Config.initialize(builder.Configuration);
builder.Services.AddHostedService<Service>();

var host = builder.Build();

if (args.Length == 1 && args[0] == "console") {
    Console.WriteLine("Starting");
    await host.RunAsync();
} else {
    try {
        host.Run();
    } catch (Exception e) {
        Log.error("Error", e);
    }
}
