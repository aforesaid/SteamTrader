using Newtonsoft.Json;

namespace SteamTrader.Core.Services.ApiClients.AntiCaptca.Requests.CreateTask
{
    public class ApiCreateTaskResponse
    {
        [JsonProperty("errorId")] public long ErrorId { get; set; }

        [JsonProperty("taskId")] public long TaskId { get; set; }
    }
}