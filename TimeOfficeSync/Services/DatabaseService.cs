using Microsoft.Data.SqlClient;
using TimeOfficeSync.Models;

namespace TimeOfficeSync.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _table;
    private readonly ILogger<DatabaseService> _logger;

    private HashSet<string>? _columns;
    private string? _insertSql;
    private string? _dedupeWhere;
    private bool _schemaFailed;

    private static readonly string[] DesiredColumns =
    {
        "EmpCode", "EntryDate", "TicketNo", "InOutFlag", "EntryTime",
        "TrfFlag", "UpdateUID", "Location", "ErrMsg", "attendance_type"
    };

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration["DatabaseSettings:ConnectionString"] ?? "";
        _logger = logger;
        _table = configuration["DatabaseSettings:tablename"] ?? "Attenifo";
    }

    public async Task<int> SavePunchDataAsync(List<PunchData> punchDataList)
    {
        int recordsSaved = 0;

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            _logger.LogInformation("Connected to database");

            if (!await EnsureSchemaAsync(connection))
                return 0;

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
                    var entryTime = new DateTime(
                        1900, 1, 1,
                        punchDateTime.Hour,
                        punchDateTime.Minute,
                        punchDateTime.Second
                    );

                    var inOutFlag = "Z";
                    if (!string.IsNullOrEmpty(punchData.M_Flag))
                    {
                        var flag = punchData.M_Flag.ToUpper();
                        if (flag == "I" || flag == "O")
                            inOutFlag = flag;
                    }

                    using (var command = new SqlCommand(_insertSql, connection))
                    {
                        AddParam(command, "EmpCode", punchData.Empcode);
                        AddParam(command, "EntryDate", entryDate);
                        AddParam(command, "InOutFlag", inOutFlag);
                        AddParam(command, "EntryTime", entryTime);
                        AddParam(command, "TrfFlag", "0");
                        AddParam(command, "UpdateUID", string.IsNullOrWhiteSpace(punchData.Empcode) ? DBNull.Value : punchData.Empcode);
                        AddParam(command, "Location", Truncate(punchData.Location, 100));
                        AddParam(command, "ErrMsg", DBNull.Value);
                        AddParam(command, "TicketNo", DBNull.Value);
                        AddParam(command, "attendance_type", Truncate(punchData.AttendanceType, 50));

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

        _logger.LogInformation("Saved {Count} new punch records to {Table} table", recordsSaved, _table);
        return recordsSaved;
    }

    private static void AddParam(SqlCommand command, string name, object value)
    {
        if (!command.Parameters.Contains("@" + name))
            command.Parameters.AddWithValue("@" + name, value);
    }

    private static object Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DBNull.Value;
        var v = value.Trim();
        return v.Length > maxLength ? v[..maxLength] : v;
    }

    private async Task<bool> EnsureSchemaAsync(SqlConnection connection)
    {
        if (_insertSql != null)
            return true;
        if (_schemaFailed)
            return false;

        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(
                @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @Table", connection))
            {
                cmd.Parameters.AddWithValue("@Table", _table);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0));
                }
            }

            _columns = columns;

            if (!columns.Contains("EmpCode") || !columns.Contains("EntryDate"))
            {
                _schemaFailed = true;
                _logger.LogError("Table [{Table}] missing mandatory columns (EmpCode, EntryDate). Present: {Cols}",
                    _table, string.Join(", ", columns));
                return false;
            }

            var present = DesiredColumns.Where(columns.Contains).ToArray();

            var colList = string.Join(", ", present.Select(c => $"[{c}]"));
            var parmList = string.Join(", ", present.Select(c => $"@{c}"));

            var dedupeCols = new List<string> { "EmpCode", "EntryDate" };
            if (columns.Contains("EntryTime"))
                dedupeCols.Add("EntryTime");

            _dedupeWhere = string.Join(" AND ", dedupeCols.Select(c => $"[{c}] = @{c}"));

            _insertSql = $@"
                IF NOT EXISTS (SELECT 1 FROM [{_table}] WHERE {_dedupeWhere})
                BEGIN
                    INSERT INTO [{_table}] ({colList})
                    VALUES ({parmList})
                END";

            var missing = DesiredColumns.Where(c => !columns.Contains(c)).ToArray();
            if (missing.Length > 0)
            {
                _logger.LogInformation("Table [{Table}] missing optional columns (skipped): {Missing}",
                    _table, string.Join(", ", missing));
            }

            _logger.LogInformation("Insert SQL for [{Table}]: {Sql}", _table, _insertSql.Replace("\r", " ").Replace("\n", " "));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to probe schema for table [{Table}]", _table);
            return false;
        }
    }

    public async Task<int> GetTotalRecordsCountAsync()
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand($"SELECT COUNT(*) FROM [{_table}]", connection))
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
