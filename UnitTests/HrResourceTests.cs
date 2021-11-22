using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Core;
using NUnit.Framework;
using FluentAssertions;

namespace UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TestCase("2021/11/18 00:00:00")]
        [TestCase("2021/11/18 10:00:00")]
        [TestCase("2021/11/20 00:00:00")]
        [TestCase("2021/11/20 09:00:00")]
        [TestCase("2021/11/21 09:00:00")]
        [TestCase("2021/11/22 17:21:00")]
        public async Task CanPunchCardCorrectly(DateTime startMonitorTime)
        {
            var service = new HrResourceService(new ConsoleLogger(), new VoidPunchService());
            var interFace = (IHrResourceService) service;
            var endDay = startMonitorTime.Date.AddDays(1);

            var totalSecs = (int) (endDay - startMonitorTime).TotalSeconds;

            var totalDays = 8;
            foreach (var day in Enumerable.Range(0, totalDays))
            {
                var start = startMonitorTime.AddDays(day);

                foreach (var sec in Enumerable.Range(0, totalSecs))
                {
                    var now = start.AddSeconds(sec);

                    bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

                    if (now == DateTime.Parse("2021/11/23 上午 10:00:00"))
                    {
                        Debugger.Break();
                    }

                    await service.PunchCardIfNeedAsync(now);

                    interFace.NextPunchedInTime.Should().NotBeNull();
                    interFace.NextPunchedOutTime.Should().NotBeNull();

                    if (IsWeekend())
                    {
                        interFace.PunchedInTime.Should().Be(null, because: $"now time: {now}");
                        interFace.PunchedOutTime.Should().Be(null, $"now time: {now}");
                        interFace.PunchedInTime.Should().Be(null, $"now time: {now}");
                        interFace.PunchedOutTime.Should().Be(null, $"now time: {now}");
                        continue;
                    }

                    if (now.Hour < 9)
                    {
                        interFace.PunchedInTime.Should().Be(null, because: $"now time: {now}");
                        interFace.PunchedOutTime.Should().Be(null, $"now time: {now}");
                    }
                    
                    if (start.Hour > 9)
                    {
                        interFace.PunchedInTime.Should().Be(null, because: $"now time: {now}");

                    }   

                    if (now.Hour > 9  && start.Hour <= 9)
                    {
                        interFace.PunchedInTime.Should().NotBeNull($"now time: {now}");
                    }

                    if (now.Hour > 18  && start.Hour <= 18)
                    {
                        interFace.PunchedOutTime.Should().NotBeNull($"now time: {now}");
                    }
                    if (start.Hour > 18)
                    {
                        interFace.PunchedOutTime.Should().BeNull( $"now time: {now}");

                    }   
                    AssertWorkTimeShouldBeCorrect(interFace, now,start);

                    AssertOffTimeShouldBeCorrect(interFace, now,start);

                    if (sec % 3600 == 0)
                    {
                        Console.WriteLine($"[{now}]  {interFace.ToLogInfo()} ");

                    }
                }
   
                //Console.WriteLine($"Completed {interFace.ToLogInfo()} ");

             
            }
        }

        private static void AssertOffTimeShouldBeCorrect(IHrResourceService interFace, DateTime now,DateTime startMonitorTime)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
            
            var shouldHavePunchTime = now>=interFace.NextPunchedOutTime ;
            var log = $"now:{now} isweekend : {IsWeekend()} info:{interFace.ToLogInfo()}";

            
            if (IsWeekend())
            {
                interFace.PunchedOutTime.Should().BeNull(log);
                interFace.NextPunchedOutTime.Should().NotBeNull(log);
                return;
            }
            if (!shouldHavePunchTime)
            {
                interFace.PunchedOutTime.Should().BeNull(log);
                interFace.NextPunchedOutTime.Should().BeBefore(now.Date.AddHours(19),log);
                interFace.NextPunchedOutTime.Should().BeAfter(now.Date.AddHours(18.5),log);
                
                return;
            }
            interFace.PunchedOutTime.Should().NotBeNull(log);
            interFace.NextPunchedOutTime.Should().BeBefore(now.Date.AddHours(19),log);
            interFace.NextPunchedOutTime.Should().BeAfter(now.Date.AddHours(18),log);
            interFace.PunchedOutTime.Should().BeBefore(now.Date.AddHours(19),log);
            interFace.PunchedOutTime.Should().BeAfter(now.Date.AddHours(18),log);
            
            

        }

        private static void AssertWorkTimeShouldBeCorrect(IHrResourceService interFace, DateTime now,DateTime startMonitorTime)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
            var shouldHavePunchTime = now>=interFace.NextPunchedInTime && startMonitorTime.Hour<=9;

            var log = $"now:{now} isweekend : {IsWeekend()} info:{interFace.ToLogInfo()}";
            
            if (IsWeekend())
            {
                interFace.PunchedInTime.Should().BeNull(log);
                interFace.NextPunchedInTime.Should().NotBeNull(log);
                return;
            }

            if (!shouldHavePunchTime)
            {
                interFace.PunchedInTime.Should().BeNull(log);
                var nextTime =startMonitorTime.Day == now.Day && startMonitorTime.Hour>9?  
                     now.AddDays(1):now;
                
                interFace.NextPunchedInTime.Should().BeBefore(nextTime.Date.AddHours(10),log);
                interFace.NextPunchedInTime.Should().BeAfter(nextTime.Date.AddHours(9),log);
                
                return;
            }

            interFace.PunchedInTime.Should().NotBeNull(log);
            interFace.NextPunchedInTime.Should().BeBefore(now.Date.AddHours(10),log);
            interFace.NextPunchedInTime.Should().BeAfter(now.Date.AddHours(9),log);
            interFace.PunchedInTime.Should().BeBefore(now.Date.AddHours(10),log);
            interFace.PunchedInTime.Should().BeAfter(now.Date.AddHours(9),log);
        }
    }
}