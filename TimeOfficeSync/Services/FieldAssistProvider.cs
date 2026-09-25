using System.Globalization;
using System.Text;
using System.Text.Json;
using TimeOfficeSync.Models;

namespace TimeOfficeSync.Services;

public class FieldAssistProvider : IPunchDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FieldAssistProvider> _logger;
    private readonly ApiLogService _apiLog;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FieldAssistProvider(HttpClient httpClient, IConfiguration configuration, ILogger<FieldAssistProvider> logger, ApiLogService apiLog)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiLog = apiLog;
    }

    public async Task<List<PunchData>> GetPunchDataAsync(DateTime fromDate, DateTime toDate)
    {
        var all = new List<PunchData>();

        var baseUrl = _configuration["ApiSettings:FieldAssist:BaseUrl"]
            ?? "https://api.fieldassist.in/api/data/daySummary/list";
        var username = _configuration["ApiSettings:FieldAssist:Username"] ?? "";
        var password = _configuration["ApiSettings:FieldAssist:Password"] ?? "";
        var isCompanyEmployee = _configuration.GetValue<bool>("ApiSettings:FieldAssist:IsCompanyEmployee", false);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var start = fromDate.Date;
        var end = toDate.Date;
        if (start > end) (start, end) = (end, start);

        _logger.LogInformation("FieldAssist sync from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}", start, end);

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var dateStr = day.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            var url = $"{baseUrl}?date={dateStr}&isEndDate=false&isCompanyEmployee={(isCompanyEmployee ? "true" : "false")}";
            var requestTime = DateTime.Now;

            try
            {
                _logger.LogInformation("Fetching FieldAssist day summary: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("FieldAssist API HTTP {Status} for {Date}", response.StatusCode, dateStr);
                    await _apiLog.LogApiRequestAsync(url, requestTime, "Fail", 0, $"HTTP {(int)response.StatusCode}");
                    continue;
                }

                var rows = JsonSerializer.Deserialize<List<FieldAssistDaySummary>>(content, JsonOptions);
                if (rows == null)
                {
                    await _apiLog.LogApiRequestAsync(url, requestTime, "Fail", 0, "Null response");
                    continue;
                }

                var mapped = MapRows(rows);
                all.AddRange(mapped);

                await _apiLog.LogApiRequestAsync(url, requestTime, "Success", mapped.Count, null);
                _logger.LogInformation("FieldAssist {Date}: {Count} records with Login", dateStr, mapped.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching FieldAssist data for {Date}", dateStr);
                await _apiLog.LogApiRequestAsync(url, requestTime, "Fail", 0, ex.Message);
            }
        }

        return all;
    }

    private static List<PunchData> MapRows(List<FieldAssistDaySummary> rows)
    {
        var result = new List<PunchData>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Login))
                continue;
            if (string.IsNullOrWhiteSpace(row.ESMErpId))
                continue;

            if (!DateTime.TryParse(row.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var summaryDate))
                summaryDate = DateTime.MinValue;

            if (!DateTime.TryParse(row.Login, CultureInfo.InvariantCulture, DateTimeStyles.None, out var login))
                continue;

            var entryDate = summaryDate != DateTime.MinValue ? summaryDate.Date : login.Date;
            var punchDateTime = entryDate.Date.Add(login.TimeOfDay);

            result.Add(new PunchData
            {
                Empcode = row.ESMErpId.Trim(),
                Name = row.ESMName ?? "",
                PunchDate = punchDateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                M_Flag = "I",
                Location = string.IsNullOrWhiteSpace(row.DayStartLocation) ? null : row.DayStartLocation.Trim(),
                AttendanceType = row.Type
            });
        }

        return result;
    }
}
