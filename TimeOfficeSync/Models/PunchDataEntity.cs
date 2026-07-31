namespace TimeOfficeSync.Models;

public class PunchDataEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Empcode { get; set; } = string.Empty;
    public DateTime PunchDate { get; set; }
    public string M_Flag { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime SyncDate { get; set; }
}
