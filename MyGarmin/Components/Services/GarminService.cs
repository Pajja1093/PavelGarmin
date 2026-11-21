using MyGarmin.Components.Models;
using System.Text.Json;

namespace MyGarmin.Components.Services
{
    public class GarminService(HttpClient httpClient)
    {
        private string? _accessToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public async Task<string> GetGarminAccessToken(string apiKey)
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _accessToken;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://personal.connectapi.garmin.com/personalapitoken-service/personal-api/personal-api-key/token");
            request.Headers.Add("Authorization", $"ApiKey {apiKey}");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadAsStringAsync();

            _accessToken = tokenResponse.Trim('"');

            _tokenExpiration = DateTime.UtcNow.AddSeconds(50);

            return _accessToken;
        }

        public async Task<List<ActivityDTO>> GetMyActivitiesAsync(string apiKey)
        {
            var activityList = new List<ActivityDTO>();
            var seen = new HashSet<long>();
            const int pageSize = 100;
            const int maxPages = 100;   // pojistka (max 10 000 aktivit)

            for (var page = 0; page < maxPages; page++)
            {
                var token = await GetGarminAccessToken(apiKey);

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://personal.connectapi.garmin.com/activitylist-service/activities/search/activities?startDate=2020-01-01&start={page * pageSize}&limit={pageSize}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(result);

                var received = 0;
                var added = 0;

                foreach (var activitie in json.RootElement.EnumerateArray())
                {
                    received++;

                    var newActivity = new ActivityDTO
                    {
                        ActivityId = activitie.GetProperty("activityId").GetInt64(),
                        ActivityName = activitie.GetProperty("activityName").GetString(),
                        EventType = activitie.GetProperty("activityType").GetProperty("typeKey").GetString(),
                        StartTimeLocal = activitie.GetProperty("startTimeLocal").GetString(),
                        Distance = activitie.GetProperty("distance").GetDouble(),
                        DurationSeconds = activitie.GetProperty("duration").GetDouble(),
                        AverageSpeed = activitie.GetProperty("averageSpeed").GetDouble(),
                        AverageHR = activitie.GetProperty("averageHR").GetDouble(),
                        Calories = activitie.GetProperty("calories").GetDouble(),
                        IsPR = activitie.GetProperty("isPR").GetBoolean(),
                        IsFavorite = activitie.GetProperty("isFavorite").GetBoolean()
                    };

                    if (seen.Add(newActivity.ActivityId))
                    {
                        activityList.Add(newActivity);
                        added++;
                    }
                }

                // poslední stránka, nebo API vrací pořád to samé
                if (received < pageSize || added == 0) break;

                await Task.Delay(200);  
            }

            return activityList;
        }
    }
}