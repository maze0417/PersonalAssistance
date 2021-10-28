using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Clients;
using Core.Models;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace Core
{
    public interface IHrResourceService
    {
        DateTime? PunchedInTime { get; set; }

        DateTime? PunchedOutTime { get; set; }

        DateTime? NextPunchedInTime { get; set; }

        DateTime? NextPunchedOutTime { get; set; }

        DateTime LastMonitTime { get; set; }

        TimeSpan TotalWorkTime { get; }

        TaskStatus TaskStatus { get; set; }

        Task<PunchCardResponse> PunchCardAsync(bool isOffWork);
        void Init();
    }

    public class HrResourceService : BaseApiClient, IHrResourceService
    {
        private readonly IHrResourceService _instance;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IPunchCardService _punchCardService;
        private readonly ILogger _logger;
        private readonly Random rnd = new Random();

        public HrResourceService(ILogger logger, IPunchCardService punchCardService) : base(logger)
        {
            _logger = logger;
            _punchCardService = punchCardService;
            _instance = this;
        }

        DateTime? IHrResourceService.PunchedInTime { get; set; }
        DateTime? IHrResourceService.PunchedOutTime { get; set; }

        DateTime? IHrResourceService.NextPunchedInTime { get; set; }

        DateTime? IHrResourceService.NextPunchedOutTime { get; set; }
        DateTime IHrResourceService.LastMonitTime { get; set; }

        TimeSpan IHrResourceService.TotalWorkTime => _instance.PunchedInTime.HasValue
            ? DateTime.Now - _instance.PunchedInTime.Value
            : TimeSpan.MinValue;


        TaskStatus IHrResourceService.TaskStatus { get; set; }


        Task<PunchCardResponse> IHrResourceService.PunchCardAsync(bool isOffWork)
        {
            if (isOffWork)
            {
                _logger.LogInformation($"手動打卡下班 時間 : {DateTime.Now} ");
                return _punchCardService.PunchCardOffWorkAsync();
            }

            _logger.LogInformation($"手動打卡上班 時間 : {DateTime.Now} ");
            return _punchCardService.PunchCardOnWorkAsync();
        }

        void IHrResourceService.Init()
        {
            var task = Task.Factory.StartNew(MonitorApiAsync, TaskCreationOptions.LongRunning);
            _instance.TaskStatus = task.Status;
        }

        private async Task MonitorApiAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    _instance.LastMonitTime = now;
                    var isCompletedPunched = _instance.PunchedOutTime.HasValue;
                    var hour = DateTime.Now.Hour;
                    var isNormalWorkTime = hour >= 9 && hour < 10;
                    var isOffWorkTime = hour >= 18;


                    if (isCompletedPunched)
                    {
                        var moreThanOneDay = DateTime.Now.Day - _instance.PunchedOutTime.Value.Day >= 1;
                        if (moreThanOneDay)
                        {
                            _instance.PunchedInTime = null;
                            _instance.PunchedOutTime = null;
                            continue;
                        }
                    }

                    if (!_instance.NextPunchedInTime.HasValue)
                    {
                        var secs = GenerateRandomPunchSeconds(true);
                        var nextTime = now.Date.AddHours(now.Hour > 9 ? 24 + 9 : 9).AddSeconds(secs);
                        _instance.NextPunchedInTime = nextTime;
                    }

                    if (!_instance.NextPunchedOutTime.HasValue)
                    {
                        var secs = GenerateRandomPunchSeconds(false);
                        var nextTime = now.Date.AddHours(now.Hour >= 18 ? 24 + 18 : 18).AddSeconds(secs);
                        _instance.NextPunchedOutTime = nextTime;
                    }


                    if (!_instance.PunchedInTime.HasValue && isNormalWorkTime && now >= _instance.NextPunchedInTime)
                    {
                        await _punchCardService.PunchCardOnWorkAsync();
                        _instance.PunchedInTime = DateTime.Now;
                        _logger.LogInformation($"九點到了，打卡上班 :{DateTime.Now} ");
                        continue;
                    }

                    if (!_instance.PunchedOutTime.HasValue && now >= _instance.NextPunchedOutTime)
                    {
                        if (_instance.TotalWorkTime.TotalHours >= 8)
                        {
                            await _punchCardService.PunchCardOffWorkAsync();
                            _instance.PunchedOutTime = DateTime.Now;
                            _logger.LogInformation($"工時八小時到了，打卡下班 :{DateTime.Now} ");
                            continue;
                        }

                        if (isOffWorkTime)
                        {
                            await _punchCardService.PunchCardOffWorkAsync();
                            _instance.PunchedOutTime = DateTime.Now;
                            _logger.LogInformation($"七點到了...打卡下班 時間 : {DateTime.Now} ");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                finally
                {
                    Delay(_cts.Token);
                }
            }
        }

        private static void Delay(CancellationToken token)
        {
            WaitASecondOrThrowIfCanceled(token);
        }

        private static void WaitASecondOrThrowIfCanceled(CancellationToken token)
        {
            token.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
            token.ThrowIfCancellationRequested();
        }

        private int GenerateRandomPunchSeconds(bool isPunchIn)
        {
            return isPunchIn ? rnd.Next(0, 1800) : rnd.Next(1801, 3300);
        }
    }
}