using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WordPressPageBlock
{
  public List<string> Lines;
}

public static class WordPressService
{
  public static async Task<List<string>> GetPageContent()
  {
    Console.WriteLine($"Asking wordpress for content of page {Constants.WordPressPageId}");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Get, $"wp/v2/pages/{Constants.WordPressPageId}?context=edit");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      Console.WriteLine("wordpress's response: " + result);
      if (response.IsSuccessStatusCode)
      {
        var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        var content = (string)jsonRes["content"]["raw"];
        return content.GetLines();
      }
      else
      {
        throw new Exception($"GetPageContent({Constants.WordPressPageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
  
  public static async Task SetPageContent(List<string> newContent)
  {
    Console.WriteLine($"Posting new page content to wordpress.");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Post, $"wp/v2/pages/{Constants.WordPressPageId}");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
        
      var body = new JObject()
      {
        ["content"] = string.Join("\r\n", newContent)
      }.ToString();
      Console.WriteLine("Body content to send: " + body);

      request.Content = new StringContent(body, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      Console.WriteLine("wordpress's response: " + result);
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine("Wordpress page content successfully posted");
      }
      else
      {
        throw new Exception($"SetPageContent({Constants.WordPressPageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
}