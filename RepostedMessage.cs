using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RepostedMessage
{
  // these indicate where the data came from
  public string FacebookPageName;
  public string FacebookMessage;
  public string FacebookPictureUrl;

  // these indicate where the data was stored, and what it looked like once it was stored
  //public string WordPressSite;
  //public string WordPressPageId;
  //public string WordPressPageContent;

  public static RepostedMessage Load()
  {
    if (File.Exists(Constants.LastRepostedMessageFileName))
    {
      Console.WriteLine("Loading last reposted message from " + Constants.LastRepostedMessageFileName);
      var text = File.ReadAllText(Constants.LastRepostedMessageFileName);
      return JsonConvert.DeserializeObject<RepostedMessage>(text, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
    }
    else
    {
      Console.WriteLine("First time reposting a message!");
      return new RepostedMessage();
    }
  }

  public bool IsSameAs(string facebookMessage, string facebookPictureUrl)
  {
    return FacebookPageName == Constants.FacebookPageName &&
      FacebookMessage == facebookMessage &&
      FacebookPictureUrl == facebookPictureUrl;
  }

  public void Save(string newFacebookMessage, string facebookPictureUrl)
  {
    this.FacebookPageName = Constants.FacebookPageName;
    this.FacebookMessage = newFacebookMessage;
    this.FacebookPictureUrl = facebookPictureUrl;

    Console.WriteLine("Saving last reposted message to " + Constants.LastRepostedMessageFileName);
    var text = JsonConvert.SerializeObject(this, Formatting.Indented);
    File.WriteAllText(Constants.LastRepostedMessageFileName, text);
  }
}
