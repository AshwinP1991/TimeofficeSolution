using TimeOfficeSync.Services;

namespace TimeOfficeSync;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly LicenseService _licenseService;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, ApiService apiService, DatabaseService databaseService, LicenseService licenseService, IConfiguration configuration)
    {
        _logger = logger;
        _apiService = apiService;
        _databaseService = databaseService;
        _licenseService = licenseService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimeOfficeSync Worker starting at: {time}", DateTimeOffset.Now);

        var runOnceNow = _configuration.GetValue<bool>("ScheduleSettings:RunOnceNow", false);

        if (!runOnceNow)
        {
            var intervalMinutes = _configuration.GetValue<int>("ScheduleSettings:RunIntervalMinutes", 5);
            _logger.LogInformation("Waiting {Minutes} minutes before first sync", intervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await _licenseService.IsLicenseValidAsync())
            {
                _logger.LogWarning("LICENSE EXPIRED. Stopping sync.");
                break;
            }

            try
            {
                await RunSyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during sync");
            }

            var interval = _configuration.GetValue<int>("ScheduleSettings:RunIntervalMinutes", 5);
            _logger.LogInformation("Next sync in {Minutes} minutes", interval);
            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }

        _logger.LogInformation("TimeOfficeSync Worker stopping at: {time}", DateTimeOffset.Now);
    }

    private async Task RunSyncAsync()
    {
        _logger.LogInformation("Starting sync at: {time}", DateTimeOffset.Now);

        var lastSyncTime = await _databaseService.GetLastSyncTimeAsync();
        var toDate = DateTime.Now;
        DateTime fromDate;

        if (lastSyncTime.HasValue)
        {
            fromDate = lastSyncTime.Value;
            _logger.LogInformation("Using last sync time as FromDate: {FromDate}", fromDate);
        }
        else
        {
            var fromDateDaysBack = _configuration.GetValue<int>("ApiSettings:FromDateDaysBack", 30);
            fromDate = toDate.AddDays(-fromDateDaysBack);
            _logger.LogInformation("First run, using {Days} days back as FromDate: {FromDate}", fromDateDaysBack, fromDate);
        }

        var punchData = await _apiService.GetPunchDataAsync(fromDate, toDate);

        if (punchData.Count > 0)
        {
            var recordsSaved = await _databaseService.SavePunchDataAsync(punchData);
            _logger.LogInformation("Sync completed. {RecordsSaved} new records saved.", recordsSaved);
        }
        else
        {
            _logger.LogInformation("No new punch data to sync.");
        }

        await _databaseService.UpdateLastSyncTimeAsync(toDate);
        _logger.LogInformation("Updated last sync time to: {SyncTime}", toDate);

        var totalCount = await _databaseService.GetTotalRecordsCountAsync();
        _logger.LogInformation("Total records in Attenifo: {TotalCount}", totalCount);
    }
}
