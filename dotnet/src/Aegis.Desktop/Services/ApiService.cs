using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Aegis.Desktop.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string? _authToken;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001/api/")
        };
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    // Disappearing Messages API
    public async Task<bool> SetDisappearingMessagesAsync(Guid conversationId, int? disappearAfterSeconds)
    {
        try
        {
            var request = new
            {
                conversationId,
                disappearAfterSeconds
            };

            var response = await _httpClient.PostAsJsonAsync("conversations/disappearing-messages", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error setting disappearing messages: {ex.Message}");
            return false;
        }
    }

    public async Task<int?> GetDisappearingMessagesSettingAsync(Guid conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"conversations/{conversationId}/disappearing-messages");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DisappearingMessagesResponse>();
                return result?.DisappearAfterSeconds;
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting disappearing messages setting: {ex.Message}");
            return null;
        }
    }

    // Privacy Settings API
    public async Task<bool> UpdatePrivacySettingsAsync(Guid conversationId, PrivacySettings settings)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"conversations/{conversationId}/privacy", settings);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating privacy settings: {ex.Message}");
            return false;
        }
    }
}

public class DisappearingMessagesResponse
{
    public int? DisappearAfterSeconds { get; set; }
}

public class PrivacySettings
{
    public bool SealedSenderEnabled { get; set; }
    public bool TypingIndicatorsEnabled { get; set; }
    public bool ReadReceiptsEnabled { get; set; }
}
