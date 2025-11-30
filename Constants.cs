using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Constants
{
  static Constants()
  {
    var jsonText = File.ReadAllText(ConstantsFileName);
    var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
    var constants = JsonConvert.DeserializeObject<JObject>(jsonText, settings);
    string GetConstant(string name)
    {
      return (string)constants[name] ?? throw new Exception(string.Format("missing {0} from {1}", name, ConstantsFileName));
    }

    FacebookAppId = GetConstant("FacebookAppId");
    if (string.IsNullOrEmpty(FacebookAppId.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(FacebookAppId)}");
    }
    
    FacebookAppSecret = GetConstant("FacebookAppSecret");
    if (string.IsNullOrEmpty(FacebookAppSecret.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(FacebookAppSecret)}");
    }
    
    FacebookPageName = GetConstant("FacebookPageName");
    if (string.IsNullOrEmpty(FacebookPageName.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(FacebookPageName)}");
    }

    FacebookLoginRedirectCertFilePath = GetConstant("FacebookLoginRedirectCertFilePath");
    if (string.IsNullOrEmpty(FacebookLoginRedirectCertFilePath)) FacebookLoginRedirectCertFilePath = "private_resource_self_signed_cert.pfx";

    FacebookLoginRedirectCertPassword = GetConstant("FacebookLoginRedirectCertPassword");

    var facebookLoginRedirectListeningPortString = GetConstant("FacebookLoginRedirectListeningPort");
    if (!int.TryParse(facebookLoginRedirectListeningPortString, out FacebookLoginRedirectListeningPort))
    {
      throw new Exception($"invalid non-numeric {nameof(FacebookLoginRedirectListeningPort)} in ConstantsFileName");
    }

    BrowserExePath = GetConstant("BrowserExePath");
    if (string.IsNullOrEmpty(BrowserExePath.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(BrowserExePath)}");
    }
    
    BrowserExeArgs = GetConstant("BrowserExeArgs");
    
    WordPressAuthUsername = GetConstant("WordPressAuthUsername");
    if (string.IsNullOrEmpty(WordPressAuthUsername.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(WordPressAuthUsername)}");
    }
    
    WordPressAuthPassword = GetConstant("WordPressAuthPassword");
    if (string.IsNullOrEmpty(WordPressAuthPassword.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(WordPressAuthPassword)}");
    }
    
    WordPressSite = GetConstant("WordPressSite");
    if (string.IsNullOrEmpty(WordPressSite.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(WordPressSite)}");
    }
    
    WordPressPageFooter = GetConstant("WordPressPageFooter");
    
    WordPressPageImageNamePattern = GetConstant("WordPressPageImageNamePattern");
    if (string.IsNullOrEmpty(WordPressPageImageNamePattern.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(WordPressPageImageNamePattern)}");
    }
  }

  public static readonly string ConstantsFileName = "private_resource_constants.json";
  public static readonly string FacebookAppId;
  public static readonly string FacebookAppSecret;
  public static readonly string FacebookPageName;
  public static readonly string FacebookLoginRedirectCertFilePath;
  public static readonly string FacebookLoginRedirectCertPassword;
  public static readonly int    FacebookLoginRedirectListeningPort;
  public static readonly string FacebookUserAccessTokenFileName = "private_resource_user_access_token.json";
  public static readonly string BrowserExePath;
  public static readonly string BrowserExeArgs;
  public static readonly string WordPressAuthUsername;
  public static readonly string WordPressAuthPassword;
  public static readonly string WordPressSite;
  public static readonly string WordPressPageFooter;
  public static readonly string WordPressPageImageNamePattern;
}
