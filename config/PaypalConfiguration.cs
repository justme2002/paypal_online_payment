using PayPal.Api;

namespace asp_net_core_paypal.config;

public static class PaypalConfiguration
{
  public readonly static string? ClientId;
  public readonly static string? ClientSecret;

  static PaypalConfiguration()
  {
    
  }

  public static Dictionary<string, string> GetConfig()
  {
    return new Dictionary<string, string>()
    {
      {"mode", "sandbox"}
    };
  }

  private static string getAccessToken(
    string ClientId,
    string ClientSecret,
    Dictionary<string, string> Mode
  ) 
  {
    OAuthTokenCredential ValidateCredential = new OAuthTokenCredential(
      ClientId, 
      ClientSecret, 
      GetConfig()
    );
    string token = ValidateCredential.GetAccessToken();
    return token;
  }

  public static APIContext GetApiContext(
    string ClientId,
    string ClientSecret
  )
  {
    APIContext ApiContext = new APIContext(getAccessToken(ClientId, ClientSecret, GetConfig()));
    ApiContext.Config = GetConfig();
    return ApiContext;
  }
}