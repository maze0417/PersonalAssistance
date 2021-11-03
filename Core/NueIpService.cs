using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Clients;
using Core.Models;
using Core.Models.NueIp;
using Microsoft.Extensions.Logging;
using PunchCardApp;

namespace Core
{
    public sealed class NueIpService : BaseApiClient, IPunchCardService
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;
        private static Regex _tokenFinder = new Regex(@"(<input type=""hidden""[^>]+name=""(token.*)""+>(.*?))");
        private static Regex _tokenExtract = new Regex(@"(?<=\bvalue="")[^""]*");
        private const string LoginUrl = "https://cloud.nueip.com/login/index/param";
        private const string PunchCardUrl = "https://cloud.nueip.com/time_clocks/ajax";


        public NueIpService(ILogger logger, IAppConfiguration appConfiguration) : base(logger)
        {
            _logger = logger;
            _appConfiguration = appConfiguration;
        }

        Task<PunchCardResponse> IPunchCardService.PunchCardOnWorkAsync()
        {
            return PunchCardAsync(false);
        }

        private async Task<PunchCardResponse> PunchCardAsync(bool isOffWork)
        {
            var response = await LoginAsync();
            if (string.IsNullOrEmpty(response))
                return new PunchCardResponse
                {
                    success = false
                };

            var matches = _tokenFinder.Match(response);
            var tokenHtml = matches.Value;
            var msg = new StringBuilder($"found token html {tokenHtml}  ");

            var matchToken = _tokenExtract.Match(tokenHtml);
            var token = matchToken.Value;

            msg.Append($"found token {token}");

            if (string.IsNullOrEmpty(token))
            {
                msg.Append("found token fail !! ");

                throw new Exception(msg.ToString());
            }


            return await PunchCardAsync(token, isOffWork);
        }

        private async Task<string> LoginAsync()
        {
            var content = new LoginRequest
            {
                inputCompany = _appConfiguration.NueIpCompany,
                inputPassword = _appConfiguration.NueIpPwd,
                inputID = _appConfiguration.NueIpId
            };

            var request = new HttpRequestMessage(HttpMethod.Post, LoginUrl)
            {
                Content = content.ToFormRequest()
            };

            var response = await SendAsync(request, false);

            if (!response.Contains(_appConfiguration.NueIpId))
            {
                _logger.LogDebug($"登入{_appConfiguration.NueIpId}失敗  !!");
            }

            return response;
        }

        private async Task<PunchCardResponse> PunchCardAsync(string token, bool isOffWork)
        {
            var id = isOffWork ? "2" : "1";
            var content = new TimeClockRequest
            {
                id = id,
                lat = _appConfiguration.Lat,
                lng = _appConfiguration.Lng,
                token = token
            };

            var request = new HttpRequestMessage(HttpMethod.Post, PunchCardUrl)
            {
                Content = content.ToFormRequest()
            };

            var raw = await SendAsync(request);

            var response = JsonSerializer.Deserialize<TimeClockResponse>(raw);

            if (response?.status != "success")
            {
                return new PunchCardResponse
                {
                    success = false,
                    message = $"打卡失敗:{response?.message}"
                };
            }

            return new PunchCardResponse
            {
                success = true,
                message = "打卡ok"
            };
        }

        Task<PunchCardResponse> IPunchCardService.PunchCardOffWorkAsync()
        {
            return PunchCardAsync(true);
        }

        Task<List<string>> IPunchCardService.GetDayCardDetailAsync()
        {
            throw new NotImplementedException();
        }
    }
}