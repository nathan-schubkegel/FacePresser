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

    FacebookLoginRedirectCertFilePath = GetConstant("FacebookLoginRedirectCertFilePath");
    if (string.IsNullOrEmpty(FacebookLoginRedirectCertFilePath)) FacebookLoginRedirectCertFilePath = "private_resource_self_signed_cert.pfx";

    FacebookLoginRedirectCertPassword = GetConstant("FacebookLoginRedirectCertPassword");

    var facebookLoginRedirectListeningPortString = GetConstant("FacebookLoginRedirectListeningPort");
    if (!int.TryParse(facebookLoginRedirectListeningPortString, out FacebookLoginRedirectListeningPort))
    {
      throw new Exception($"invalid non-numeric {nameof(FacebookLoginRedirectListeningPort)} in ConstantsFileName");
    }

    BrowserExePath = GetConstant("BrowserExePath");
    BrowserExeArgs = GetConstant("BrowserExeArgs");
    WordPressAuthUsername = GetConstant("WordPressAuthUsername");
    WordPressAuthPassword = GetConstant("WordPressAuthPassword");
    WordPressSite = GetConstant("WordPressSite");
    WordPressPageId = GetConstant("WordPressPageId");
    
    WordPressPageHeadingTextWhereReplacementStarts = GetConstant("WordPressPageHeadingTextWhereReplacementStarts");
    if (string.IsNullOrEmpty(WordPressPageHeadingTextWhereReplacementStarts.Trim()))
    {
      throw new Exception($"invalid empty or whitespace {nameof(WordPressPageHeadingTextWhereReplacementStarts)}");
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
  public static readonly string FacebookImageCacheFileName = "private_resource_last_downloaded_facebook_image.json";
  public static readonly string LastRepostedMessageFileName = "private_resource_last_reposted_message.json";
  public static readonly string BrowserExePath;
  public static readonly string BrowserExeArgs;
  public static readonly string WordPressAuthUsername;
  public static readonly string WordPressAuthPassword;
  public static readonly string WordPressSite;
  public static readonly string WordPressPageId;
  public static readonly string WordPressPageHeadingTextWhereReplacementStarts;
  public static readonly string WordPressPageFooter;
  public static readonly string WordPressPageImageNamePattern;
}
