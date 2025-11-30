using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WordPressMediaItem
{
  public string Id;
  public string Url;
}

public class WordPressPost
{
  public string WordPressPostId;
  public string WordPressRawContent;
  public string FacebookPostId;
  public string FacebookPostCreatedTime;
  public string FacebookPostUpdatedTime;
}

public static class WordPressService
{
  // this can be useful to learn what routes are valid
  public static async Task<string> GetWpJson()
  {
    Console.WriteLine($"Requesting GET /wp-json...");
    using var client = new HttpClient();
    client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

    var request = new HttpRequestMessage(HttpMethod.Get, $"");
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
      Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
      Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
        
    using HttpResponseMessage response = await client.SendAsync(request);
    string result = await response.Content.ReadAsStringAsync();
    if (response.IsSuccessStatusCode)
    {
	  var jsonRes = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
	  var betterResult = jsonRes.ToString(Formatting.Indented);
      Console.WriteLine("wordpress's response: " + betterResult);
      return betterResult;
    }
    else
    {
      Console.WriteLine("wordpress's response: " + result);
      throw new Exception($"GetWpJson() failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
    }
  }

  public static async Task<List<WordPressPost>> GetWordPressPostsAfterDateTime(DateTime when)
  {
    List<WordPressPost> posts = new();
    using var client = new HttpClient();
    client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

    int fetchedCount = 0;
    int requestCount = 0;
    while (true)
    {
      requestCount++;
      Console.WriteLine($"Asking wordpress for posts after {when.ToLocalTime()} (request {requestCount})...");
      var whenText = when.ToUniversalTime().ToString("o");
      var request = new HttpRequestMessage(HttpMethod.Get, $"wp/v2/posts?orderby=date&context=edit&after={whenText}&offset={fetchedCount}&per_page=3");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonRes = JsonConvert.DeserializeObject<JArray>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          fetchedCount += jsonRes.Count;
          Console.WriteLine($"Received {jsonRes.Count} posts.");
          //Console.WriteLine(jsonRes.ToString(Formatting.Indented));
          if (jsonRes.Count == 0) break;
          foreach (var jsonPost in jsonRes)
          {
            var id = (string)jsonPost["id"];
            var content = (string)jsonPost["content"]["raw"];
            string consoleLine = null;
            foreach (var line in content.GetLines())
            {
              if (line.Trim() == "") continue;
              var match = Regex.Match(line, @"^\s*" + Regex.Escape("<!--") + @"(.*)" + Regex.Escape("-->") + @"\s*$");
              if (match.Success)
              {
                var metaText = match.Groups[1].Value.Trim();
                JObject metaJson;
                try
                {
                  metaJson = JsonConvert.DeserializeObject<JObject>(metaText, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
                  posts.Add(new WordPressPost
                  {
                    WordPressPostId = id,
                    WordPressRawContent = content,
                    FacebookPostId = (string)metaJson["facebookPostId"] ?? throw new Exception("metaJson is missing facebookPostId"),
                    FacebookPostCreatedTime = (string)metaJson["facebookPostCreatedTime"] ?? throw new Exception("metaJson is missing facebookPostCreatedTime"),
                    FacebookPostUpdatedTime = (string)metaJson["facebookPostUpdatedTime"] ?? throw new Exception("metaJson is missing facebookPostUpdatedTime"),
                  });
                  consoleLine = $"found with {metaText}";
                }
                catch (Exception ex)
                {
                  consoleLine = $"ignored because first line HTML comment caused " + ex.GetType() + ": " + ex.Message;
                }
              }
              else consoleLine = $"ignored because first line is not a HTML comment";

              break;
            }
            
            consoleLine = "  Wordpress Post {id} " + (consoleLine ?? "ignored because it has no content");
            Console.WriteLine(consoleLine);
          }
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
        throw new Exception($"GetWordPressPostsAfterDateTime({whenText}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
    if (posts.Select(x => x.WordPressPostId).Distinct().Count() != posts.Count)
    {
       throw new Exception("Foiled by time and multiple requests!");
    }
    return posts;
  }

  public static async Task<string> CreatePost(string name, DateTime postTime, List<string> contentLines, WordPressMediaItem featuredImage)
  {
    Console.WriteLine($"Creating new wordpress post \"{name}\".");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Post, $"wp/v2/posts");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
        
      var bodyArgs = new JObject()
      {
		["date_gmt"] = postTime.ToUniversalTime().ToString("o"),
		["context"] = "edit",
		["status"] = "publish",
		["title"] = name,
        ["content"] = string.Join("\r\n", contentLines),
        ["comment_status"] = "closed",
      };
      if (featuredImage != null)
      {
        bodyArgs["featured_media"] = featuredImage.Id;
	  }
      
      var body = bodyArgs.ToString();

      Console.WriteLine("/////////////////////////////////////////////////////////");
      Console.WriteLine("////////////// New WordPress post content ///////////////");
      Console.WriteLine("/////////////////////////////////////////////////////////");
      foreach (var line in contentLines) Console.WriteLine(line);

      request.Content = new StringContent(body, Encoding.UTF8, "application/json");
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      string betterResult;
      JObject jsonResult = null;
      try
      {
        jsonResult = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
	    betterResult = jsonResult.ToString(Formatting.Indented);
	  }
	  catch
	  {
        betterResult = result;
	  }
      
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine("Wordpress post content successfully posted");
        //Console.WriteLine("Wordpress's response: " + betterResult);
        return (string)jsonResult["id"];
      }
      else
      {
        Console.WriteLine("wordpress's response: " + betterResult);
        throw new Exception($"CreatePost({name}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
  
  // public static async Task OverwriteExistingPost(string postId, string name, List<string> contentLines, byte[] featuredImage)

  public static async Task<List<string>> GetPageContent(string pageId)
  {
    Console.WriteLine($"Asking wordpress for content of page {pageId}");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Get, $"wp/v2/pages/{pageId}?context=edit");
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
        throw new Exception($"GetPageContent({pageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
  
  public static async Task SetPageContent(string pageId, List<string> newContent)
  {
    Console.WriteLine($"Posting new page content to wordpress.");
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");

      var request = new HttpRequestMessage(HttpMethod.Post, $"wp/v2/pages/{pageId}");
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
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine("Wordpress page content successfully posted");
      }
      else
      {
        Console.WriteLine("wordpress's response: " + result);
        throw new Exception($"SetPageContent({pageId}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
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
        throw new Exception($"FindMediaItems({search}) failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }

  public static async Task<WordPressMediaItem> EnsureImageIsUploaded(byte[] imageContent, string identifier)
  {
    // download every wordpress image that has been uploaded by this application
    var existingItems = await FindMediaItems(Constants.WordPressPageImageNamePattern + "-" + identifier);
    WordPressMediaItem matchingItem = null;
    foreach (var item in existingItems)
    {
      var imageBytes = await DownloadMediaItem(item);
      if (imageBytes.SequenceEqual(imageContent))
      {
        Console.WriteLine("Found already-existing media item matching image content");
        matchingItem = item;
        break;
      }
    }
    
    // delete every non-matching wordpress image
    foreach (var item in existingItems.Where(x => x != matchingItem))
    {
      await DeleteMediaItem(item);
    }

    if (matchingItem == null)
    {
      // upload a new wordpress media item
      return await UploadMediaItem(imageContent, identifier);
    }
    else
    {
      return matchingItem;
    }
  }

  public static async Task<WordPressMediaItem> UploadMediaItem(byte[] fileContent, string identifier)
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
      fileName = Constants.WordPressPageImageNamePattern + "-" + identifier + "." + mimeType.Substring("image/".Length);
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
  
  public static async Task<byte[]> DownloadMediaItem(WordPressMediaItem mediaItem)
  {
    Console.WriteLine("Downloading media item from wordpress at " + mediaItem.Url);
    using (var pictureStream = new MemoryStream())
    using (var client = new HttpClient())
    {
      using HttpResponseMessage response = await client.GetAsync(mediaItem.Url);
      using var responseStream = await response.Content.ReadAsStreamAsync();
      responseStream.CopyTo(pictureStream);
      pictureStream.Position = 0;
      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine($"wordpress's response: success ({pictureStream.Length} bytes)");
        return pictureStream.ToArray();
      }
      else
      {
        using var resultReader = new StreamReader(pictureStream);
        var result = resultReader.ReadToEnd();
        Console.WriteLine("wordpress's response: " + result);
        throw new Exception($"Media item download request failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
  
  public static async Task DeleteMediaItem(WordPressMediaItem mediaItem)
  {
    Console.WriteLine("Deleting media item " + mediaItem.Id + " from wordpress at " + mediaItem.Url);
    using (var pictureStream = new MemoryStream())
    using (var client = new HttpClient())
    {
      client.BaseAddress = new Uri($"https://{Constants.WordPressSite}/wp-json/");
      
      var request = new HttpRequestMessage(HttpMethod.Delete, $"wp/v2/media/{mediaItem.Id}?force=true");
      request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
        Constants.WordPressAuthUsername + ":" + Constants.WordPressAuthPassword)));
      using HttpResponseMessage response = await client.SendAsync(request);
      string result = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
        try
        {
          var jsonItem = JsonConvert.DeserializeObject<JObject>(result, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
          if (!(bool)jsonItem["deleted"])
          {
            throw new Exception("media item was not deleted");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine("wordpress's response: " + result);
          throw new Exception($"DeleteMediaItem() failed to process response from WordPress", ex);
        }
      }
      else
      {
        Console.WriteLine("wordpress's response: " + result);
        throw new Exception($"DeleteMediaItem() failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
      }
    }
  }
}
