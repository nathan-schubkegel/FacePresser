using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WordPressMediaItem
{
  public string Id;
  public string Url;
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
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          var content = (string)jsonRes["content"]["raw"];
          var lines = content.GetLines();
          Console.WriteLine("wordpress's response (page content only):" + (lines.Count == 0 ? " (0 lines)" : ""));
          foreach (var line in lines) Console.WriteLine(line);
          return lines;
        }
        catch
        {
          Console.WriteLine("wordpress's response: " + result);
          throw;
        }
      }
      else
      {
        Console.WriteLine("wordpress's response: " + result);
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

      Console.WriteLine("/////////////////////////////////////////////////////////");
      Console.WriteLine("////////////// New WordPress page content ///////////////");
      Console.WriteLine("/////////////////////////////////////////////////////////");
      foreach (var line in newContent) Console.WriteLine(line);

      request.Content = new StringContent(body, Encoding.UTF8, "application/json");
      using HttpResponseMessage response = await client.SendAsync(request);
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

  public static async Task<List<WordPressMediaItem>> FindMediaItems(string search)
  {
    Console.WriteLine($"Asking wordpress for media items matching \"{search}\"");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      // FUTURE: use pagination to search all items, so we 100% know we're finding them all
      var request = new HttpRequestMessage(HttpMethod.Get, $"wp/v2/media?context=edit&per_page=100&search={ System.Net.WebUtility.UrlEncode(search) }");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var items = new List<WordPressMediaItem>();
          var jsonArray = JsonConvert.DeserializeObject<JArray>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          foreach (var jsonItem in jsonArray)
          {
            var id = (string)jsonItem["id"];
            var url = (string)jsonItem["source_url"];
            if (id == null || url == null) continue; // skip this one, brother
            items.Add(new WordPressMediaItem { Id = id, Url = url });
          }
          Console.WriteLine($"wordpress's response ({items.Count} of {jsonArray.Count} media items only):" + (items.Count == 0 ? " (0 items)" : ""));
          foreach (var item in items) Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
          return items;
        }
        catch
        {
          Console.WriteLine("wordpress's response: " + result);
          throw;
        }
      }
      else
      {
        Console.WriteLine("wordpress's response: " + result);
        throw new Exception($"FindMediaItems() failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }

/*
  public static async Task<(string imageId, string imageUrl)> EnsureImageIsUploaded(byte[] imageContent)
  {
    // look at all wordpress images that have been uploaded by this application
    //FindMediaItems
    
    // TODO: actually implement this
    await Task.Yield();
    return ("527", Constants.WordPressPageImageUrl);

    // also https://stackoverflow.com/questions/47478733/upload-media-to-wordpress-rest-api
    // and https://stackoverflow.com/questions/37432114/wp-rest-api-upload-image
    // and https://wordpress.stackexchange.com/questions/415506/uploading-media-to-wordpress-api-with-c-httpclient
  }
*/
  
  public static async Task<WordPressMediaItem> UploadMediaItem(byte[] fileContent)
  {
    string mimeType;
    try
    {
      mimeType = ImageTypeChecker.GetImageMimeType(fileContent);
    }
    catch (Exception ex)
    {
      throw new Exception($"Unable to upload new media item to wordpress because it is not a recognized image type", ex);
    }
    
    string fileName;
    try
    {
      fileName = Constants.WordPressPageImageNamePattern + "-" + Guid.NewGuid().ToString() + "." + mimeType.Substring("image/".Length);
    }
    catch (Exception ex)
    {
      throw new Exception($"Unable to upload new media item to wordpress because a filename could not be determined", ex);
    }

    Console.WriteLine($"Uploading new WordPress media item named {fileName}");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Post, $"wp/v2/media");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      var content = new ByteArrayContent(fileContent);
      content.Headers.Remove("Content-Type");
      content.Headers.Add("Content-Type", mimeType);
      content.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
      request.Content = content;
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonItem = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          var id = (string)jsonItem["id"];
          var url = (string)jsonItem["source_url"];
          if (id == null || url == null) throw new Exception("Invalid response id or url");
          var item = new WordPressMediaItem { Id = id, Url = url };
          Console.WriteLine("wordpress's response (media item only): " + JsonConvert.SerializeObject(item, Formatting.Indented));
          return item;
        }
        catch (Exception ex)
        {
          Console.WriteLine("wordpress's response: " + result);
          throw new Exception($"UploadMediaItem() failed to process response from WordPress", ex);
        }
      }
      else
      {
        Console.WriteLine("wordpress's response: " + result);
        throw new Exception($"UploadMediaItem() failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
}
