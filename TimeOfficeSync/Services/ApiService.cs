using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TimeOfficeSync.Models;

namespace TimeOfficeSync.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiService> _logger;
    private readonly string _connectionString;

    public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
    }

    public async Task<List<PunchData>> GetPunchDataAsync(DateTime fromDate, DateTime toDate)
    {
        var url = "";
        var requestTime = DateTime.Now;
        var status = "Fail";
        var recordsCount = 0;
        string? exceptionMsg = null;

        try
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://api.etimeoffice.com/api/DownloadPunchData";
            var username = _configuration["ApiSettings:Username"] ?? "";

            var fromDateStr = fromDate.ToString("dd/MM/yyyy_HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            var toDateStr = toDate.ToString("dd/MM/yyyy_HH:mm", System.Globalization.CultureInfo.InvariantCulture);

            url = $"{baseUrl}?Empcode=ALL&FromDate={fromDateStr}&ToDate={toDateStr}";

            _logger.LogInformation("Fetching punch data from API: {Url}", url);

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null || apiResponse.Error)
            {
                status = "Fail";
                exceptionMsg = apiResponse?.Msg ?? "Null response";
                _logger.LogWarning("API returned error: {Message}", exceptionMsg);
                await LogApiRequestAsync(url, requestTime, status, recordsCount, exceptionMsg);
                return new List<PunchData>();
            }

            recordsCount = apiResponse.PunchData.Count;
            status = "Success";

            _logger.LogInformation("Successfully fetched {Count} punch records from API", recordsCount);
            await LogApiRequestAsync(url, requestTime, status, recordsCount, null);
            return apiResponse.PunchData;
        }
        catch (Exception ex)
        {
            status = "Fail";
            exceptionMsg = ex.Message;
            _logger.LogError(ex, "Error fetching punch data from API");
            await LogApiRequestAsync(url, requestTime, status, recordsCount, exceptionMsg);
            throw;
        }
    }

    private async Task LogApiRequestAsync(string url, DateTime requestTime, string status, int recordsCount, string? exceptionMsg)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"INSERT INTO [ApiLog] ([RequestUrl], [RequestTime], [Status], [RecordsCount], [ExceptionMsg])
                        VALUES (@RequestUrl, @RequestTime, @Status, @RecordsCount, @ExceptionMsg)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RequestUrl", url);
            command.Parameters.AddWithValue("@RequestTime", requestTime);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@RecordsCount", recordsCount);
            command.Parameters.AddWithValue("@ExceptionMsg", (object?)exceptionMsg ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log API request");
        }
    }
}
