using System.Threading.Tasks;
using Core.Models;

namespace Core
{
    public class VoidPunchService : IPunchCardService
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

        public Task<(string onWorker, string offWork)> GetDayCardDetailAsync()
        {
            return Task.FromResult((string.Empty, string.Empty));
        }
    }
}