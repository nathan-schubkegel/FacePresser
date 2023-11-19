using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FacebookPagePost
{
  public string Id;
  public string FromId; // who posted it
  public string FromName; // who posted it
  public string Message;
  public string FullPicture;
}

public class FacebookPageAccount
{
  public string PageAccessToken;
  public string PageName;
  public string PageId;
}

public class FacebookPageService
{
  private string _userAccessToken;
  
  public FacebookPageService(string userAccessToken)
  {
    _userAccessToken = userAccessToken;
  }

  public async Task<List<FacebookPageAccount>> GetPageAccounts(string userId)
  {
    Console.WriteLine("Asking facebook for pages the user is admin of");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");
      HttpResponseMessage response = await client.GetAsync($"{userId}/accounts?access_token={_userAccessToken}");
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          var results = new List<FacebookPageAccount>();
          foreach (var post in jsonRes["data"])
          {
            string accessToken = post["access_token"].ToString();
            string name = post["name"].ToString();
            string id = post["id"].ToString();
            results.Add( new FacebookPageAccount { PageAccessToken = accessToken, PageId = id, PageName = name } );
          }
          Console.WriteLine("facebook's response (account info only):" + (results.Count == 0 ? " (0 accounts)" : ""));
          foreach (var thing in results) Console.WriteLine(JsonConvert.SerializeObject(thing, Formatting.Indented));
          return results;
        }
        catch
        {
          Console.WriteLine("facebook's response: " + result);
          throw;
        }
      }
      else
      {
        Console.WriteLine("facebook's response: " + result);
        throw new Exception($"GetPagesOfUser({userId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }

  public async Task<List<FacebookPagePost>> GetMostRecentPostsOnPage(string pageId, string pageAccessToken)
  {
    Console.WriteLine("Asking facebook for most recent posts on a page");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");

      HttpResponseMessage response = await client.GetAsync($"{pageId}/posts?limit=5&fields=id,from,is_expired,is_hidden,is_published,message,full_picture&access_token={pageAccessToken}");
      string result = await response.Content.ReadAsStringAsync();
      Console.WriteLine("facebook's response: " + result);
      if (response.IsSuccessStatusCode)
      {
        var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

        var results = new List<FacebookPagePost>();
        foreach (var post in jsonRes["data"])
        {
          if ((bool?)post["is_expired"] == true || (bool?)post["is_hidden"] == true) continue;
          
          if ((bool?)post["is_published"] == false) continue;
          
          results.Add(new FacebookPagePost
          { 
            Id = post["id"]?.ToString(),
            FromId = post["from"]["id"]?.ToString(),
            FromName = post["from"]["name"]?.ToString(),
            Message = post["message"]?.ToString(),
            FullPicture = post["full_picture"]?.ToString(),
          });
        }
        return results;
      }
      else
      {
        throw new Exception($"GetMostRecentPostsOnPage({pageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }

  public static async Task<(string facebookMessage, string facebookPictureUrl)> GetLatestFacebookPostAsync()
  {
    // get user access token
    var userAccessToken = FacebookUserAccessTokenService.GetUserAccessToken();
    var pageService = new FacebookPageService(userAccessToken);

    // find the pages that can be watched
    List<FacebookPageAccount> pageAccounts;
    try
    {
      pageAccounts = await pageService.GetPageAccounts("me");
    }
    catch
    {
      FacebookUserAccessTokenService.DeleteCachedUserAccessToken();
      throw;
    }
    var pageAccount = pageAccounts.FirstOrDefault(a => a.PageName == Constants.FacebookPageName);
    if (pageAccount == null)
    {
      throw new Exception($"FacebookPageName=\"{Constants.FacebookPageName}\" was specified in " +
        $"{Constants.ConstantsFileName} but the facebook user does not have admin access to any page by that name.");
    }

    // get the most recent posts
    var posts = await pageService.GetMostRecentPostsOnPage(pageAccount.PageId, pageAccount.PageAccessToken);
    Console.WriteLine("Page Posts:" + (posts.Count == 0 ? " (none)" : ""));
    foreach (var post in posts)
    {
      Console.WriteLine(JsonConvert.SerializeObject(post, Formatting.Indented));
    }
    return (posts[0].Message, posts[0].FullPicture);
  }

  public static async Task<byte[]> DownloadFacebookImageAsync(string url)
  {
    Console.WriteLine("Downloading image from facebook: " + url);
    using (var pictureStream = new MemoryStream())
    using (var client = new HttpClient())
    {
      HttpResponseMessage response = await client.GetAsync(url);
      using var responseStream = await response.Content.ReadAsStreamAsync();
      responseStream.CopyTo(pictureStream);
      pictureStream.Position = 0;
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine($"facebook's response: success ({pictureStream.Length} bytes)");
        return pictureStream.ToArray();
      }
      else
      {
        using var resultReader = new StreamReader(pictureStream);
        var result = resultReader.ReadToEnd();
        Console.WriteLine("facebook's response: " + result);
        throw new Exception($"Picture download request failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
}
