using Microsoft.Data.SqlClient;
using TimeOfficeSync.Models;

namespace TimeOfficeSync.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
        _logger = logger;
    }

    public async Task<int> SavePunchDataAsync(List<PunchData> punchDataList)
    {
        int recordsSaved = 0;

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            _logger.LogInformation("Connected to database");

            foreach (var punchData in punchDataList)
            {
                try
                {
                    if (!DateTime.TryParseExact(punchData.PunchDate, new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var punchDateTime))
                    {
                        _logger.LogWarning("Failed to parse date: {PunchDate}", punchData.PunchDate);
                        continue;
                    }

                    var entryDate = punchDateTime.Date;
                    var entryTime = punchDateTime.TimeOfDay;

                    // Determine InOutFlag based on M_Flag or time
                    var inOutFlag = "Z"; // Default no value
                    if (!string.IsNullOrEmpty(punchData.M_Flag))
                    {
                        var flag = punchData.M_Flag.ToUpper();
                        if (flag == "I" || flag == "O")
                            inOutFlag = flag;
                    }

                    var sql = @"
                        IF NOT EXISTS (SELECT 1 FROM [Attenifo] WHERE [EmpCode] = @EmpCode AND [EntryDate] = @EntryDate AND [EntryTime] = @EntryTime)
                        BEGIN
                            INSERT INTO [Attenifo] ([EmpCode], [EntryDate], [InOutFlag], [EntryTime], [TrfFlag], [UpdateUID], [Location], [ErrMsg])
                            VALUES (@EmpCode, @EntryDate, @InOutFlag, @EntryTime, @TrfFlag, @UpdateUID, @Location, @ErrMsg)
                        END";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@EmpCode", punchData.Empcode);
                        command.Parameters.AddWithValue("@EntryDate", entryDate);
                        command.Parameters.AddWithValue("@InOutFlag", inOutFlag);
                        command.Parameters.AddWithValue("@EntryTime", entryTime);
                        command.Parameters.AddWithValue("@TrfFlag", "0");
                        command.Parameters.AddWithValue("@UpdateUID", DBNull.Value);
                        command.Parameters.AddWithValue("@Location", DBNull.Value);
                        command.Parameters.AddWithValue("@ErrMsg", DBNull.Value);

                        var result = await command.ExecuteNonQueryAsync();
                        if (result > 0)
                        {
                            recordsSaved++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving punch data for {Empcode}", punchData.Empcode);
                }
            }
        }

        _logger.LogInformation("Saved {Count} new punch records to Attenifo table", recordsSaved);
        return recordsSaved;
    }

    public async Task<int> GetTotalRecordsCountAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand("SELECT COUNT(*) FROM [Attenifo]", connection))
            {
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand("SELECT [LastSyncTime] FROM [ApiSyncStatus] WHERE [Id] = 1", connection))
            {
                var result = await command.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return null;
                return Convert.ToDateTime(result);
            }
        }
    }

    public async Task UpdateLastSyncTimeAsync(DateTime syncTime)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"IF EXISTS (SELECT 1 FROM [ApiSyncStatus] WHERE [Id] = 1)
                        BEGIN
                            UPDATE [ApiSyncStatus] 
                            SET [LastSyncTime] = @SyncTime, [ModifiedDate] = GETDATE() 
                            WHERE [Id] = 1
                        END
                        ELSE
                        BEGIN
                            INSERT INTO [ApiSyncStatus] ([Id], [LastSyncTime], [CreatedDate], [ModifiedDate])
                            VALUES (1, @SyncTime, GETDATE(), GETDATE())
                        END";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@SyncTime", syncTime);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
