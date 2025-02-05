using System.Drawing;
using System.Text;
using System.Timers;
using Reloaded.Mod.Interfaces.Internal;
using System.Timers;

namespace RNSReloaded.CoyoteRabbit;

public class CoyoteHttpClient {

    private static HttpClient _client = new HttpClient();
    private static Task<HttpResponseMessage> _currentTask;
    private static string FireUrl = "";
    private static string _postUrl = "";
    private static ILoggerV1 Logger = null!;

    private static System.Timers.Timer _timer;
    private static float _deltaStrength = 0;
    private static bool _isExecuting = false;
    private static readonly object _lock = new object(); // 用于线程同步

    public static void Init(string baseUrl, string clientId, ILoggerV1 logger)
    {
        FireUrl = $"{baseUrl}api/game/{clientId}/fire";
        Logger = logger;
        _postUrl = $"{baseUrl}api/v2/game/{clientId}/strength";

        _timer = new System.Timers.Timer();
        _timer.AutoReset = false; // 不自动重复，只触发一次
        _timer.Elapsed += OnTimerElapsed;
    }

    private static async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            try
            {
                var jsonContent = $@"
                {{
                    ""strength"": {{
                        ""sub"": {_deltaStrength}
                    }}
                }}";
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Logger.PrintMessage($"恢复强度 -{_deltaStrength}", Color.Blue);

                var response = _client.PostAsync(_postUrl, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    Logger.PrintMessage("请求成功: " + response.Content.ReadAsStringAsync().Result, Color.Green);
                }
                else
                {
                    Logger.PrintMessage("请求失败: " + response.StatusCode, Color.Red);
                }
            }
            catch (Exception ex)
            {
                Logger.PrintMessage("请求异常: " + ex.Message, Color.Red);
            }
            _isExecuting = false;
        }
    }

    public static async void Fire(float strength, float duration) {
        var url = _postUrl;
        if (string.IsNullOrEmpty(url))
            return;
        // 如果计时器正在运行，则停止并重新启动
        if (Monitor.TryEnter(_lock, TimeSpan.Zero)) // 非阻塞锁
        {
            try
            {
                if (_timer.Enabled) {
                    _timer.Stop();
                    Logger.PrintMessage("已有正在等待的定时器，重新启动", Color.Yellow);
                    // 设置定时器时长
                    _timer.Interval = duration * 1000; // 转为毫秒单位
                    _timer.Start();
                    return;
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
        else
        {
            Logger.PrintMessage("已有正在运行的定时器，忽略", Color.Yellow);
            return;
        }
        //下面的代码保证没有在途的定时器运行。
        var jsonContent = $@"
                    {{
                        ""strength"": {{
                            ""add"": {strength}
                        }}
                    }}";

        HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        var response =  _client.PostAsync(url, content);
        if (_currentTask != null && _currentTask.IsCompleted)
        {
            Logger.PrintMessage("Prev Damage is not end.", Color.Blue);
            _currentTask = null;
        }
        _timer.Interval = duration * 1000; // 转为毫秒单位
        _deltaStrength = strength;
        _timer.Start();

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
