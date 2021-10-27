using System;
using System.Collections.Generic;
using System.Linq;
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
        IList<DateTime> CachedPunchTime { get; set; }

        DateTime LastMonitTime { get; set; }

        TimeSpan WorkerTime { get; }
        TimeSpan CacheInterval { get; }
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

        public HrResourceService(ILogger logger, IPunchCardService punchCardService) : base(logger)
        {
            _logger = logger;
            _punchCardService = punchCardService;
            _instance = this;
        }

        IList<DateTime> IHrResourceService.CachedPunchTime { get; set; } = new List<DateTime>();

        DateTime IHrResourceService.LastMonitTime { get; set; }

        TimeSpan IHrResourceService.WorkerTime => _instance.CachedPunchTime.Count == 0
            ? TimeSpan.MinValue
            : DateTime.Now - _instance.CachedPunchTime.First();

        TimeSpan IHrResourceService.CacheInterval => _instance.CachedPunchTime.Count == 0
            ? TimeSpan.MinValue
            : _instance.CachedPunchTime.Last() - _instance.CachedPunchTime.First();

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
                    _instance.LastMonitTime = DateTime.Now;
                    var cachePunchTime = _instance.CachedPunchTime;

                    if (cachePunchTime.Count > 0)
                    {
                        var moreThanOneDay = (DateTime.Now - cachePunchTime.Last()).TotalDays >= 1;
                        if (moreThanOneDay)
                        {
                            cachePunchTime.Clear();
                            continue;
                        }
                    }


                    if (cachePunchTime.Count == 0
                        || cachePunchTime.Count < 2
                        || _instance.CacheInterval.Hours < 9
                    )
                    {
                        if (_instance.WorkerTime.TotalHours >= 9)
                        {
                            await _punchCardService.PunchCardOffWorkAsync();
                            _instance.CachedPunchTime.Add(DateTime.Now);
                            _logger.LogInformation($"工時九小時到了，打卡下班 :{DateTime.Now} ");
                            continue;
                        }

                        var hour = DateTime.Now.Hour;
                        if (hour >= 9 && hour < 18)
                        {
                            await _punchCardService.PunchCardOnWorkAsync();
                            _instance.CachedPunchTime.Add(DateTime.Now);
                            _logger.LogInformation($"九點到了...打卡上班 時間 : {DateTime.Now} ");
                            continue;
                        }

                        if (hour >= 18)
                        {
                            await _punchCardService.PunchCardOffWorkAsync();
                            _instance.CachedPunchTime.Add(DateTime.Now);
                            _logger.LogInformation($"六點到了...打卡下班 時間 : {DateTime.Now} ");
                            continue;
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
    }
}