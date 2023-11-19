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

  public async Task<FacebookPagePost> GetMostRecentPostOnPage(string pageId, string pageAccessToken)
  {
    Console.WriteLine("Asking facebook for most recent post on page");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");

      // TODO: perform paginated requests until a satisfactory post is found
      HttpResponseMessage response = await client.GetAsync($"{pageId}/posts?limit=5&fields=id,from,is_expired,is_hidden,is_published,message,full_picture&access_token={pageAccessToken}");
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          foreach (var post in jsonRes["data"])
          {
            if ((bool?)post["is_expired"] == true || (bool?)post["is_hidden"] == true) continue;
            
            if ((bool?)post["is_published"] == false) continue;
            
            var pagePost = new FacebookPagePost
            { 
              Id = post["id"]?.ToString(),
              FromId = post["from"]["id"]?.ToString(),
              FromName = post["from"]["name"]?.ToString(),
              Message = post["message"]?.ToString(),
              FullPicture = post["full_picture"]?.ToString(),
            };

            Console.WriteLine("facebook's response (found page post only): " + JsonConvert.SerializeObject(pagePost, Formatting.Indented));
            return pagePost;
          }
          throw new Exception("Didn't find any usable posts");
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
        throw new Exception($"GetMostRecentPostOnPage({pageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
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

    // get the most recent post
    var post = await pageService.GetMostRecentPostOnPage(pageAccount.PageId, pageAccount.PageAccessToken);
    return (post.Message, post.FullPicture);
  }
}
