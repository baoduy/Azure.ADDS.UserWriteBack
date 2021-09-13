using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.ADDS.UserWriteBack.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Azure.ADDS.UserWriteBack.MsGraph
{
    public class GraphService
    {
        private readonly ILogger<GraphService> _logger;
        private readonly AzAdSyncOptions _options;
        private AuthenticationResult _token;
        private readonly GraphServiceClient _client;

        public GraphService(IOptions<AzAdSyncOptions> options, ILogger<GraphService> logger)
        {
            _logger = logger;
            _options = options.Value;
            _client = new GraphServiceClient(new DelegateAuthenticationProvider(async requestMessage =>
            {
                if (_token == null)
                {
                    var app = ConfidentialClientApplicationBuilder.Create(_options.ClientId)
                        .WithClientSecret(_options.ClientSecret)
                        .WithAuthority(new Uri(_options.Authority))
                        .Build();

                    _token = await app.AcquireTokenForClient(new[] {"https://graph.microsoft.com/.default"})
                        .ExecuteAsync();
                }

                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            }));
        }

        public async Task<IEnumerable<UserRecord>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var filter = string.Join(" or ", _options.AzureAdGroups.Select(g => $"startsWith(displayName, '{g}')"));

            _logger.LogInformation("Get Az AD Group information: {0}", string.Join(",", _options.AzureAdGroups));

            var groups = await _client.Groups.Request()
                .Filter(filter)
                .Select(s => new {s.DisplayName, s.Id})
                .GetAsync(cancellationToken);

            if (groups == null)
            {
                _logger.LogInformation("Cannot get Az AD groups information.");
                return Array.Empty<UserRecord>();
            }

            var rs = await Task.WhenAll(groups.Select(g =>
                _client.Groups[g.Id].Members.Request()
                    .Select(u => new {u.Id}).GetAsync(cancellationToken)));
            var userIds = rs.SelectMany(r => r.Select(u => u.Id));

            _logger.LogInformation("Get Az AD Users information: {0}", string.Join(",", _options.AzureAdGroups));

            var users = (await Task.WhenAll(userIds.Select(i =>
                    _client.Users[i].Request()
                        .Select(u => new
                            {u.DisplayName, u.GivenName, u.Surname, u.Mail, u.ProxyAddresses, u.UserPrincipalName})
                        .GetAsync(cancellationToken))))
                .Select(u => new UserRecord(u.UserPrincipalName, u.DisplayName, u.GivenName, u.Surname, u.Mail,
                    u.ProxyAddresses.ToArray()));

            //Recreate access token for next call
            _token = null;
            return users;
        }
    }
}