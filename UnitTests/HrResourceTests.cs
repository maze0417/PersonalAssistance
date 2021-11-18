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

        [Test]
        public async Task CanPunchCardCorrectly()
        {
            var service = new HrResourceService(new ConsoleLogger(), new VoidPunchService());
            var interFace = (IHrResourceService) service;

            var startDay = DateTime.Parse("2021/11/18 00:00:00");
            var endDay = DateTime.Parse("2021/11/18 23:59:59");

            var totalSecs = (int) (endDay - startDay).TotalSeconds;

            var totalDays = 32;
            foreach (var day in Enumerable.Range(0, totalDays))
            {
                var start = startDay.AddDays(day);

                foreach (var sec in Enumerable.Range(0, totalSecs))
                {
                    var now = start.AddSeconds(sec);

                    bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

                    if (now == DateTime.Parse("2021/12/1 上午 12:00:00"))
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

                    if (now.Hour > 9)
                    {
                        interFace.PunchedInTime.Should().NotBeNull($"now time: {now}");
                    }

                    if (now.Hour > 18)
                    {
                        interFace.PunchedOutTime.Should().NotBeNull($"now time: {now}");
                    }
                }


                AssertWorkTimeShouldBeCorrect(interFace, start);

                AssertOffTimeShouldBeCorrect(interFace, start);
            }
        }

        private static void AssertOffTimeShouldBeCorrect(IHrResourceService interFace, DateTime now)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

            if (IsWeekend())
            {
                interFace.PunchedOutTime.Should().BeNull();
                interFace.NextPunchedOutTime.Should().NotBeNull();
                Console.WriteLine($"today is weekend ,   {now} ,next off time is {interFace.NextPunchedOutTime}");
                return;
            }

            interFace.PunchedOutTime.Should().NotBeNull();
            interFace.NextPunchedOutTime.Should().BeBefore(now.AddHours(19));
            interFace.NextPunchedOutTime.Should().BeAfter(now.AddHours(18));
            interFace.PunchedOutTime.Should().BeBefore(now.AddHours(19));
            interFace.PunchedOutTime.Should().BeAfter(now.AddHours(18));
            Console.WriteLine($"off time is {interFace.PunchedOutTime}");
        }

        private static void AssertWorkTimeShouldBeCorrect(IHrResourceService interFace, DateTime now)
        {
            bool IsWeekend() => now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

            if (IsWeekend())
            {
                interFace.PunchedInTime.Should().BeNull();
                interFace.NextPunchedInTime.Should().NotBeNull();
                Console.WriteLine($"today is weekend ,   {now},next work time is {interFace.NextPunchedInTime} ");
                return;
            }

            interFace.PunchedInTime.Should().NotBeNull();
            interFace.NextPunchedInTime.Should().BeBefore(now.AddHours(10));
            interFace.NextPunchedInTime.Should().BeAfter(now.AddHours(9));
            interFace.PunchedInTime.Should().BeBefore(now.AddHours(10));
            interFace.PunchedInTime.Should().BeAfter(now.AddHours(9));
            Console.WriteLine($"work time is {interFace.PunchedInTime}");
        }
    }
}