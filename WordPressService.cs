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
        var jsonRes = JsonConvert.DeserializeObject<dynamic>(result);
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
        // yay I guess
      }
      else
      {
        throw new Exception($"SetPageContent({Constants.WordPressPageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
  
  
        // The content is blocks of commented HTML.
        //   - the newlines are a distraction - you gotta watch for the open/close comments
        //   - and they can be nested
        //   - and they can be one-line comments
/*
<!-- wp:paragraph -->
<p class=\"\">Food Rescue of Sky Valley hosts a community food share program as an important means of preventing food waste and giving to our community. We aim to reduce waste by getting these products into the community to help others before they need to be given to animals or compost. </p>
<!-- /wp:paragraph -->

<!-- wp:paragraph -->
<p class=\"\">How it works:</p>
<!-- /wp:paragraph -->

<!-- wp:list -->
<ul class=\"\"><!-- wp:list-item -->
<li class=\"\">You request a food share box. Fill out the form below or visit our community Facebook page at <a class=\"rank-math-link\" href=\"https://www.facebook.com/foodrescueofskyvalley/\">https://www.facebook.com/foodrescueofskyvalley/</a> and comment on a \"share out\" post or send a direct message.</li>
<!-- /wp:list-item -->

<!-- wp:list-item -->
<li class=\"\">We send you a direct message (facebook) or email (non-facebook) confirming location and your assigned pick up time.</li>
<!-- /wp:list-item -->

<!-- wp:list-item -->
<li class=\"\">You come at your assigned time and receive a box of food.</li>
<!-- /wp:list-item --></ul>
<!-- /wp:list -->

<!-- wp:wpforms/form-selector {\"clientId\":\"d4558162-58db-4323-b373-e17a32aea993\",\"formId\":\"296\",\"copyPasteJsonValue\":\"{\\u0022displayTitle\\u0022:false,\\u0022displayDesc\\u0022:false,\\u0022fieldSize\\u0022:\\u0022medium\\u0022,\\u0022fieldBorderRadius\\u0022:\\u00223px\\u0022,\\u0022fieldBackgroundColor\\u0022:\\u0022#ffffff\\u0022,\\u0022fieldBorderColor\\u0022:\\u0022rgba( 0, 0, 0, 0.25 )\\u0022,\\u0022fieldTextColor\\u0022:\\u0022rgba( 0, 0, 0, 0.7 )\\u0022,\\u0022labelSize\\u0022:\\u0022medium\\u0022,\\u0022labelColor\\u0022:\\u0022rgba( 0, 0, 0, 0.85 )\\u0022,\\u0022labelSublabelColor\\u0022:\\u0022rgba( 0, 0, 0, 0.55 )\\u0022,\\u0022labelErrorColor\\u0022:\\u0022#d63637\\u0022,\\u0022buttonSize\\u0022:\\u0022medium\\u0022,\\u0022buttonBorderRadius\\u0022:\\u00223px\\u0022,\\u0022buttonBackgroundColor\\u0022:\\u0022#066aab\\u0022,\\u0022buttonTextColor\\u0022:\\u0022#ffffff\\u0022}\"} /-->
*/
}