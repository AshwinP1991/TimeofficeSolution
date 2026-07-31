namespace TimeOfficeSync.Models;

public class ApiResponse
{
    public bool Error { get; set; }
    public string Msg { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<PunchData> PunchData { get; set; } = new();
}

public class PunchData
{
    public string Name { get; set; } = string.Empty;
    public string Empcode { get; set; } = string.Empty;
    public string PunchDate { get; set; } = string.Empty;
    public string M_Flag { get; set; } = string.Empty;
}
