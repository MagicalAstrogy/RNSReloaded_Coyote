using System.Drawing;
using Reloaded.Mod.Interfaces.Internal;

namespace RNSReloaded.CoyoteRabbit;

public class CoyoteHttpClient {

    private static HttpClient _client = new HttpClient();
    private static Task<HttpResponseMessage> _currentTask;
    private static string FireUrl = "";
    private static ILoggerV1 Logger = null!;

    public static void Init(string baseUrl, string clientId, ILoggerV1 logger)
    {
        FireUrl = $"{baseUrl}api/game/{clientId}/fire";
        Logger = logger;
    }

    public static async void Fire(float strength, float duration) {
        var url = FireUrl;
        if (string.IsNullOrEmpty(url))
            return;
        var jsonContent = $@"
                    {{
                        ""strength"": {strength},
                        ""time"": {(int)(duration * 1000)}
                    }}";
        HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        var response =  _client.PostAsync(url, content);
        if (_currentTask != null && _currentTask.IsCompleted)
        {
            Logger.PrintMessage("Prev Damage is not end.", Color.Blue);
            _currentTask = null;
        }

        if (_currentTask == null)
            _currentTask = response;
        else
        {
            /*
            _currentTask.ContinueWith(finished_task =>
            {
                if (finished_task != null)
                    Logger.Log(finished_task.Result.Content?.ReadAsStringAsync()?.Result);
                _currentTask = null;
                _currentTask = response;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            */
        }
        //Logger.Log(response.Content.ReadAsStringAsync().Result);
        //no wait.
    }
}
