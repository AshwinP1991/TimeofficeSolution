using TimeOfficeSync;
using TimeOfficeSync.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SoftoLogsync";
});

// Register services
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<LicenseService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
