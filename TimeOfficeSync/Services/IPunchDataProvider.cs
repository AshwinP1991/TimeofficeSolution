using TimeOfficeSync.Models;

namespace TimeOfficeSync.Services;

public interface IPunchDataProvider
{
    Task<List<PunchData>> GetPunchDataAsync(DateTime fromDate, DateTime toDate);
}
