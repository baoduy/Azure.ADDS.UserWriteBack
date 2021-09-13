using System.Collections.Generic;

namespace Azure.ADDS.UserWriteBack.Options
{
    public class AzAdSyncOptions
    {
        public static string Name => "AzAdSync";
        
        public string DomainName { get; set; }
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AdOrgUnit { get; set; }
        public string AdGroup { get; set; }
        
        public string DefaultPassword { get; set; }
        public ICollection<string> AzureAdGroups { get; set; } = new List<string>();
        public ICollection<string> ExcludedUsers { get; set; } = new List<string>();
    }
}