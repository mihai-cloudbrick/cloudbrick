#nullable enable
using Cloudbrick;
using System.Text.Json.Nodes;
namespace Cloudbrick.Orleans.Reminders.DataExplorer;
internal sealed class ReminderDocument
{
    public string ServiceId { get; init; } = "";
    public string GrainId { get; init; } = "";
    public string ReminderName { get; init; } = "";
    public DateTime StartAtUtc { get; init; }
    public TimeSpan Period { get; init; }
    public uint GrainHash { get; init; }
    public int Bucket { get; init; }
    public int Version { get; init; } = 1;
    public JsonObject ToJson() => new(){ ["ServiceId"]=ServiceId, ["GrainId"]=GrainId, ["ReminderName"]=ReminderName, ["StartAtUtc"]=StartAtUtc, ["PeriodMs"]=Period.TotalMilliseconds, ["GrainHash"]=(long)GrainHash, ["Bucket"]=Bucket, ["Version"]=Version };
    public static ReminderDocument FromJson(JsonObject o)=> new(){ ServiceId=o["ServiceId"]!.GetValue<string>(), GrainId=o["GrainId"]!.GetValue<string>(), ReminderName=o["ReminderName"]!.GetValue<string>(), StartAtUtc=o["StartAtUtc"]!.GetValue<DateTime>(), Period=TimeSpan.FromMilliseconds(o["PeriodMs"]!.GetValue<double>()), GrainHash=unchecked((uint)o["GrainHash"]!.GetValue<long>()), Bucket=o["Bucket"]!.GetValue<int>(), Version=o["Version"]?.GetValue<int>() ?? 1 };
}
