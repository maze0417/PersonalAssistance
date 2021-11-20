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
        public async Task CanPunchCardCorrectly(DateTime startDay)
        {
            var service = new HrResourceService(new ConsoleLogger(), new VoidPunchService());
            var interFace = (IHrResourceService) service;
            var endDay = startDay.Date.AddDays(1);

            var totalSecs = (int) (endDay - startDay).TotalSeconds;

            var totalDays = 32;
            foreach (var day in Enumerable.Range(0, totalDays))
            {
                var start = startDay.AddDays(day);

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
                }


                AssertWorkTimeShouldBeCorrect(interFace, start,startDay);

                AssertOffTimeShouldBeCorrect(interFace, start,startDay);
            }
        }

        private static void AssertOffTimeShouldBeCorrect(IHrResourceService interFace, DateTime now,DateTime startDay)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
         
            
            if (IsWeekend())
            {
                interFace.PunchedOutTime.Should().BeNull();
                interFace.NextPunchedOutTime.Should().NotBeNull();
                Console.WriteLine($"{now} today is weekend,next off time is {interFace.NextPunchedOutTime}");
                return;
            }

            interFace.PunchedOutTime.Should().NotBeNull();
            interFace.NextPunchedOutTime.Should().BeBefore(now.Date.AddHours(19));
            interFace.NextPunchedOutTime.Should().BeAfter(now.Date.AddHours(18));
            interFace.PunchedOutTime.Should().BeBefore(now.Date.AddHours(19));
            interFace.PunchedOutTime.Should().BeAfter(now.Date.AddHours(18));
            
            Console.WriteLine($"{now} today is work day  {interFace.PunchedOutTime} ,next work time is {interFace.NextPunchedOutTime} ");

        }

        private static void AssertWorkTimeShouldBeCorrect(IHrResourceService interFace, DateTime now,DateTime startDay)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
            var isAfterWorkHour = startDay.Hour >= 10;
        
            if (IsWeekend())
            {
                interFace.PunchedInTime.Should().BeNull();
                interFace.NextPunchedInTime.Should().NotBeNull();
                Console.WriteLine($"{now} today is weekend ,next work time is {interFace.NextPunchedInTime} ");
                return;
            }

            if (isAfterWorkHour)
            {
                interFace.PunchedInTime.Should().BeNull();
                interFace.NextPunchedInTime.Should().BeBefore(now.Date.AddHours(10));
                interFace.NextPunchedInTime.Should().BeAfter(now.Date.AddHours(9));
                Console.WriteLine($"{now} today is work day  {interFace.PunchedInTime} ,next work time is {interFace.NextPunchedInTime} ");
                return;
            }

            interFace.PunchedInTime.Should().NotBeNull();
            interFace.NextPunchedInTime.Should().BeBefore(now.Date.AddHours(10));
            interFace.NextPunchedInTime.Should().BeAfter(now.Date.AddHours(9));
            interFace.PunchedInTime.Should().BeBefore(now.Date.AddHours(10));
            interFace.PunchedInTime.Should().BeAfter(now.Date.AddHours(9));
            
            Console.WriteLine($"{now} today is work day  {interFace.PunchedInTime} ,next work time is {interFace.NextPunchedInTime} ");

        }
    }
}