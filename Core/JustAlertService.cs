using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core
{
    public class JustAlertService : IPunchCardService
    {
        private static readonly Task<PunchCardResponse> EmptyTask = Task.FromResult(new PunchCardResponse());

        public Task<PunchCardResponse> PunchCardOnWorkAsync()
        {
            return EmptyTask;
        }

        public Task<PunchCardResponse> PunchCardOffWorkAsync()
        {
            return EmptyTask;
        }

        public Task<List<string>> GetDayCardDetailAsync()
        {
            return Task.FromResult(new List<string>());
        }
    }
}