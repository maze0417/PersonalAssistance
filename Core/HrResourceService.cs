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

        private const int StartHour = 9;
        private const int OffHour = 18;

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
            var task = Task.Factory.StartNew(() => MonitorApiAsync(), TaskCreationOptions.LongRunning);
            _instance.TaskStatus = task.Status;
        }

        private async Task MonitorApiAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await PunchCardIfNeedAsync(null);
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

        public async Task PunchCardIfNeedAsync(DateTime? nowTime)
        {
            var now = nowTime ?? DateTime.Now;


            _instance.LastMonitTime = now;
            var hour = now.Hour;
            var isNormalWorkTime = hour == StartHour;
            var isNormalOffTime = hour == OffHour;


            SetNextPunchTime(now);


            if (!_instance.PunchedInTime.HasValue && isNormalWorkTime && now >= _instance.NextPunchedInTime)
            {
                
                await _punchCardService.PunchCardOnWorkAsync();
                _instance.PunchedInTime = now;
                _logger.LogInformation($"九點到了，打卡上班 :{now} ");
                return;
            }

            if (!_instance.PunchedOutTime.HasValue && isNormalOffTime && now >= _instance.NextPunchedOutTime)
            {
                await _punchCardService.PunchCardOffWorkAsync();
                _instance.PunchedOutTime = now;
                _logger.LogInformation($"七點到了...打卡下班 時間 : {now} ");
            }
        }


        private void SetNextPunchTime(DateTime now)
        {
            if (!_instance.NextPunchedInTime.HasValue)
            {
                SetNextTime(true);
               
            }

            if (!_instance.NextPunchedOutTime.HasValue)
            {
                SetNextTime(false);
            }

            var isTomorrow =
                _instance.NextPunchedInTime != null && (now - _instance.NextPunchedInTime.Value.Date).TotalDays >= 1;

            if (!isTomorrow)
            {
                return;
            }

            _instance.PunchedInTime = null;
            _instance.PunchedOutTime = null;
            SetNextTime(true);
            SetNextTime(false);
            _logger.LogInformation($"跨天重算下次打卡時間:{now} ");

            void SetNextTime(bool isPunchIn)
            {
              
                var isWeekend = false;
                var nextTime =now;
                
                bool IsWeekend() => nextTime.DayOfWeek == DayOfWeek.Saturday || nextTime.DayOfWeek == DayOfWeek.Sunday;

                while (IsWeekend())
                {
                    nextTime = nextTime.AddDays(1);
                    isWeekend = true;
                }
                var secs = GenerateRandomPunchSeconds(true);
                
                 // nextTime =isWeekend ?  
                 //     nextTime.Date.AddHours(isPunchIn? StartHour : OffHour).AddSeconds(secs): 
                 //     nextTime.AddDays(1).Date.AddHours(isPunchIn? StartHour: OffHour).AddSeconds(secs);
                 nextTime = nextTime.Date.AddHours(isPunchIn ? StartHour : OffHour).AddSeconds(secs);
                if (isPunchIn)
                {
                    _instance.NextPunchedInTime = nextTime;    
                }
                if (!isPunchIn)
                {
                    _instance.NextPunchedOutTime = nextTime;    
                }

                _logger.LogInformation($"下次上班打卡時間:{nextTime} ");
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