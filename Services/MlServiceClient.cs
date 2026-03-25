using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RetailForecast.DTOs.TrainingRun;
using RetailForecast.Settings;

namespace RetailForecast.Services
{
    public class MlServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly MlServiceSettings _settings;

        public MlServiceClient(HttpClient httpClient, IOptions<MlServiceSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<MlTrainingStartResponse?> StartTrainingAsync(MlTrainingStartRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new InvalidOperationException("ML service base URL is not configured");

            var endpoint = new Uri(new Uri(EnsureTrailingSlash(_settings.BaseUrl)), "api/trainingrun/start");
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClient.SendAsync(message, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"ML service returned {(int)response.StatusCode}: {error}",
                    null,
                    response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<MlTrainingStartResponse>(cancellationToken: ct);
        }

        private static string EnsureTrailingSlash(string value)
            => value.EndsWith("/") ? value : $"{value}/";
    }
}
