using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class WordPressService
{
  public static async Task GetPageContent(string pageId)
  {
    Console.WriteLine($"Asking wordpress for content of page {pageId}");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");
      //client.DefaultRequestHeaders.Add("Authorization", "Basic Y29kZW1hemU6aXN0aGViZXN0");

      var request = new HttpRequestMessage(HttpMethod.Get, $"wp/v2/pages/{pageId}?context=edit");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      Console.WriteLine("wordpress's response: " + result);
      if (response.IsSuccessStatusCode)
      {
        /*var jsonRes = JsonConvert.DeserializeObject<dynamic>(result);
        
        var results = new List<FacebookPageAccount>();
        foreach (var post in jsonRes["data"])
        {
          string accessToken = post["access_token"].ToString();
          string name = post["name"].ToString();
          string id = post["id"].ToString();
          results.Add( new FacebookPageAccount { PageAccessToken = accessToken, PageId = id, PageName = name } );
        }
        return results;*/
      }
      else
      {
        throw new Exception($"GetPageContent({pageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
}