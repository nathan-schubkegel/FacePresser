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
    FacebookAppSecret = GetConstant("FacebookAppSecret");
    FacebookPageName = GetConstant("FacebookPageName");
    FacebookListenerCertName = GetConstant("FacebookListenerCertName");
    FacebookListenerCertPassword = GetConstant("FacebookListenerCertPassword");
    BrowserExePath = GetConstant("BrowserExePath");
    BrowserExeArgs = GetConstant("BrowserExeArgs");
    WordPressAuthUsername = GetConstant("WordPressAuthUsername");
    WordPressAuthPassword = GetConstant("WordPressAuthPassword");
    WordPressSite = GetConstant("WordPressSite");
  }

  public static readonly string ConstantsFileName = "private_resource_constants.json";
  public static readonly string FacebookAppId;
  public static readonly string FacebookAppSecret;
  public static readonly string FacebookPageName;
  public static readonly string FacebookListenerCertName;
  public static readonly string FacebookListenerCertPassword;
  public static readonly string FacebookListenerCertFileName = "private_resource_cert.pfx";
  public static readonly int    FacebookLoginRedirectListenerPort = 11337;
  public static readonly string FacebookUserAccessTokenFileName = "private_resource_user_access_token.txt";
  public static readonly string BrowserExePath;
  public static readonly string BrowserExeArgs;
  public static readonly string WordPressAuthUsername;
  public static readonly string WordPressAuthPassword;
  public static readonly string WordPressSite;
}