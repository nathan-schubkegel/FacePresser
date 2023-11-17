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
      Console.WriteLine("facebook's response: " + result);
      if (response.IsSuccessStatusCode)
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
        return results;
      }
      else
      {
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
}