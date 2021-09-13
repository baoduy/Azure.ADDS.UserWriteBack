using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using Azure.ADDS.UserWriteBack.MsGraph;
using Azure.ADDS.UserWriteBack.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PasswordGenerator;

namespace Azure.ADDS.UserWriteBack.Adds
{
    public class AdService
    {
        private readonly ILogger<AdService> _logger;
        private readonly AzAdSyncOptions _options;
        private readonly IPassword _passGenerator;

        public AdService(IOptions<AzAdSyncOptions> options, ILogger<AdService> logger)
        {
            _logger = logger;
            _options = options.Value;
            _passGenerator = new Password().IncludeLowercase().IncludeUppercase().IncludeSpecial().LengthRequired(50);
        
        }

        private PrincipalContext TryConnect(string container=null)
        {
            try
            {
                return new PrincipalContext(ContextType.Domain, _options.DomainName, container, ContextOptions.Negotiate);
            }
            catch
            {
                try
                {
                    return new PrincipalContext(ContextType.Machine,  _options.DomainName, container, ContextOptions.Negotiate);
                }
                catch
                {
                    return new PrincipalContext(ContextType.ApplicationDirectory,  _options.DomainName, container, ContextOptions.Negotiate);
                }
            }
        }

        public void Upserts(IEnumerable<UserRecord> users)
        {
            _logger.LogInformation("ADDS Unsert users");
            
            // set up domain context
            using var domain = TryConnect();
            
            //Find the OU
            using var org = string.IsNullOrEmpty(_options.AdOrgUnit) ? null : TryConnect(_options.AdOrgUnit);
            
            //Find the Group
            using var group = string.IsNullOrWhiteSpace(_options.AdGroup)
                ? null :GroupPrincipal.FindByIdentity(domain,IdentityType.DistinguishedName,_options.AdGroup);
            
            //_logger.LogInformation("ADDS Domain: {0}", domain.ConnectedServer);
            //_logger.LogInformation("ADDS Organization Unit: {0}", org?.ConnectedServer);
            
            foreach (var u in users.Where(u => !_options.ExcludedUsers.Contains(u.UserId)))
            {
                var user = UserPrincipal.FindByIdentity(domain, IdentityType.UserPrincipalName, u.UserId);

                if (user != null)
                {
                    //_logger.LogInformation("ADDS create found: {0}", user.DistinguishedName);
                    if (group!=null && !group.Members.Contains(user))
                    {
                        _logger.LogInformation("ADDS Add member: {DistinguishedName} to group {Name}", user.DistinguishedName, group.Name);
                        
                        group.Members.Add(user);
                        group.Save();
                    }

                    user.Enabled = true;
                    user.UnlockAccount();
                    user.Dispose();
                    continue;
                }

                _logger.LogInformation("ADDS create new user: {0}", u.UserId);
                
                var samUserId = u.UserId.Replace("@Transwap.com", string.Empty,
                    StringComparison.CurrentCultureIgnoreCase);
                
                user = new UserPrincipal(org ?? domain, samUserId, string.IsNullOrWhiteSpace(_options.DefaultPassword)?_passGenerator.Next():_options.DefaultPassword, false);

                user.UserPrincipalName = u.UserId;
                user.DisplayName = u.DisplayName;
                user.EmailAddress = u.Email;

                user.GivenName = u.GivenName;
                user.Surname = u.Surname;
                user.PasswordNeverExpires = true;
                //Save user to load the proxyAddresses property.
                user.Save();

                if (group!=null)
                {
                    _logger.LogInformation("ADDS Add member: {DistinguishedName} to group {Name}", user.DistinguishedName, group.Name);
                    group.Members.Add(user);
                    group.Save();
                }
                
                _logger.LogInformation("ADDS update proxyAddresses for new users: {UserId}", u.UserId);
                var obj = (System.DirectoryServices.DirectoryEntry) user.GetUnderlyingObject();
                var prop = obj.Properties["proxyAddresses"];
                prop.Value = u.ProxyAddress;

                //Only enable user if Proxy address is updated successfully.
                user.Enabled = true;
                user.Save();
                user.UnlockAccount();
                user.Dispose();
            }
        }
    }
}