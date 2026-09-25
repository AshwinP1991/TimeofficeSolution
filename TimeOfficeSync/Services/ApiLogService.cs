using Microsoft.Data.SqlClient;

namespace TimeOfficeSync.Services;

public class ApiLogService
{
    private readonly string _connectionString;
    private readonly ILogger<ApiLogService> _logger;

    public ApiLogService(IConfiguration configuration, ILogger<ApiLogService> logger)
    {
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
        _logger = logger;
    }

    public async Task LogApiRequestAsync(string url, DateTime requestTime, string status, int recordsCount, string? exceptionMsg)
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
