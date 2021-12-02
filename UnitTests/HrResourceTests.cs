using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using NUnit.Framework;
using FluentAssertions;
using NSubstitute;
using PunchCardApp;

namespace UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public  void CanQueryDaily()
        {
            var config = Substitute.For<IAppConfiguration>();
            config.NueIpCompany.Returns("507641428");
            config.NueIpId.Returns("GJ0080");
            config.NueIpPwd.Returns("");
            var service = new HrResourceService(new ConsoleLogger(), new NueIpService(new Logger(),config));
            var interFace = (IHrResourceService) service;

            Console.WriteLine(interFace.PunchedInTime);
        }

        [TestCase("2021/11/18 00:00:00")]
        [TestCase("2021/11/18 10:00:00")]
        [TestCase("2021/11/20 00:00:00")]
        [TestCase("2021/11/20 09:00:00")]
        [TestCase("2021/11/21 09:00:00")]
        [TestCase("2021/11/22 17:21:00")]
        [TestCase("2021/11/22 19:13:00")]
        [TestCase("2021/11/30 00:13:00")]
        [TestCase("2021/12/31 00:13:00")]
        [TestCase("2021/12/1 20:13:00")]
        public async Task CanPunchCardCorrectly(DateTime startMonitorTime)
        {
            var service = new HrResourceService(new ConsoleLogger(), new VoidPunchService());
            var interFace = (IHrResourceService) service;
            var endDay = startMonitorTime.AddDays(1);

            var totalMins = (int) (endDay - startMonitorTime).TotalMinutes;

            var totalDays = 8;
            foreach (var day in Enumerable.Range(0, totalDays))
            {
                var start = startMonitorTime.AddDays(day);

                foreach (var min in Enumerable.Range(0, totalMins))
                {
                    var now = start.AddMinutes(min);

                    bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;


                    await service.PunchCardIfNeedAsync(now);
                    if (min % 60 == 0)
                    {
                        Console.WriteLine($@"[{now}]  {interFace.ToLogInfo()} ");

                    }
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

                    var crossDay =now.Year - startMonitorTime.Year >= 1 || now.Month - startMonitorTime.Month >= 1|| now.Day - startMonitorTime.Day >= 1  ? now.Date : startMonitorTime;
                    
                    AssertWorkTimeShouldBeCorrect(interFace, now,crossDay);

                    AssertOffTimeShouldBeCorrect(interFace, now,crossDay);

                  
                }
   
             
            }
        }

        private static void AssertOffTimeShouldBeCorrect(IHrResourceService interFace, DateTime now,DateTime startMonitorTime)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
            
            var shouldHavePunchTime = now>=interFace.NextPunchedOutTime && startMonitorTime.Hour<=18;
            

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
                var nextTime =startMonitorTime.Day == now.Day && startMonitorTime.Hour>18?  
                    now.AddDays(1):now;
                
                interFace.NextPunchedOutTime.Should().BeBefore(nextTime.Date.AddHours(19),log);
                interFace.NextPunchedOutTime.Should().BeAfter(nextTime.Date.AddHours(18.5),log);

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
                
                interFace.NextPunchedInTime.Should().BeOnOrBefore(nextTime.Date.AddHours(10),log);
                interFace.NextPunchedInTime.Should().BeOnOrAfter(nextTime.Date.AddHours(9),log);
                
                return;
            }

            interFace.PunchedInTime.Should().NotBeNull(log);
            interFace.NextPunchedInTime.Should().BeOnOrBefore(now.Date.AddHours(10),log);
            interFace.NextPunchedInTime.Should().BeOnOrAfter(now.Date.AddHours(9),log);
            interFace.PunchedInTime.Should().BeOnOrBefore(now.Date.AddHours(10),log);
            interFace.PunchedInTime.Should().BeOnOrAfter(now.Date.AddHours(9),log);
        }
    }
}