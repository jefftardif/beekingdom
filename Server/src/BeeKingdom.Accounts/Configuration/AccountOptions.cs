namespace BeeKingdom.Accounts.Configuration;

public sealed class AccountOptions
{
    public const string SectionName = "Accounts";

    public string DefaultLanguage { get; set; } = "en-US";
    public string DefaultTimeZone { get; set; } = "UTC";
    public string DefaultCountry { get; set; } = "US";
    public string DefaultCurrency { get; set; } = "USD";
}
