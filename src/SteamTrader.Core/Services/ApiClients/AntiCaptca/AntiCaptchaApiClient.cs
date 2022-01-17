using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamTrader.Core.Services.ApiClients.AntiCaptca.Requests.CreateTask;
using SteamTrader.Core.Services.ApiClients.AntiCaptca.Requests.GetTaskResult;

namespace SteamTrader.Core.Services.ApiClients.AntiCaptca
{
    public class AntiCaptchaApiClient : IAntiCaptchaApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;

        public AntiCaptchaApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task<long> CreateTask(string apiKey, string base64, string type, int maxLength = 4,
            int minLength = 4, int containsNumeric = 2)
        {
            var uri = AntiCaptchaEndpoints.PostCreateTaskUri;
            var taskDetails = new ApiCreateTaskRequestDetails(type, base64, minLength, maxLength, containsNumeric);
            var request = new ApiCreateTaskRequest(apiKey, taskDetails);

            var result = await PostAsync<ApiCreateTaskRequest, ApiCreateTaskResponse>(uri, request);

            return result.TaskId;
        }

        public async Task<string> GetTaskResult(string apiKey, long taskId)
        {
            const string uri = AntiCaptchaEndpoints.PostGetTaskResultUri;
            var request = new ApiGetTaskResultRequest(apiKey, taskId);

            var result = await PostAsync<ApiGetTaskResultRequest, ApiGetTaskResultResponse>(uri, request);
            if (result.ErrorId != 0)
            {
                throw new ArgumentException("Error with AntiCaptchaClient");
            }

            const string processingStatus = "processing";
            const string readyStatus = "ready";

            return result.Status switch
            {
                processingStatus => null,
                readyStatus => result.Solution.Text,
                _ => throw new ArgumentException($"Unsupported status {result.Status}")
            };
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest body)
        {
            const string contentType = "application/json";

            var bodyJson = JsonConvert.SerializeObject(body);

            var requestModel = new StringContent(bodyJson, Encoding.UTF8, contentType);
            var response = await _httpClient.PostAsync(uri, requestModel);

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseString);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class AntiCaptchaTypes
    {
        public const string ImageToTextTask = "ImageToTextTask";
    }

    public static class AntiCaptchaEndpoints
    {
        public const string PostCreateTaskUri = "https://api.anti-captcha.com/createTask";
        public const string PostGetTaskResultUri = "https://api.anti-captcha.com/getTaskResult";
    }
}