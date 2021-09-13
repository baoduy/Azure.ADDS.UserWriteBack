namespace Azure.ADDS.UserWriteBack.MsGraph
{
    public record UserRecord(string UserId,string DisplayName,string GivenName,string Surname, string Email,  string[] ProxyAddress);
}