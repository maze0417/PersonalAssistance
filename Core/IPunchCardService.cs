using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core
{
    public interface IPunchCardService
    {
        Task<PunchCardResponse> PunchCardOnWorkAsync();

        Task<PunchCardResponse> PunchCardOffWorkAsync();

        Task<(string onWorker, string offWork)> GetDayCardDetailAsync();
    }
}