using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

public static class FacebookImageCachingService
{
  private class CachedImage
  {
    public byte[] Bytes;
    public string Url;
  }

  private static CachedImage _cachedImage;

  public static async Task<byte[]> GetImageAsync(string facebookPictureUrl)
  {
    if (_cachedImage == null)
    {
      if (File.Exists(Constants.FacebookImageCacheFileName))
      {
        Console.WriteLine($"Loading cached image from {Constants.FacebookImageCacheFileName}");
        try
        {
          var text = File.ReadAllText(Constants.FacebookImageCacheFileName);
          _cachedImage = JsonConvert.DeserializeObject<CachedImage>(text, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error reading {Constants.FacebookImageCacheFileName}; will ignore file. Error was: " + ex);
          _cachedImage = new CachedImage();
        }
      }
      else
      {
        Console.WriteLine($"No cached image exists at {Constants.FacebookImageCacheFileName}");
        _cachedImage = new CachedImage();
      }
    }

    if (_cachedImage.Url == facebookPictureUrl)
    {
      Console.WriteLine($"Found facebook image in local cache");
      return _cachedImage.Bytes;
    }

    _cachedImage.Bytes = await DownloadFacebookImageAsync(facebookPictureUrl);
    _cachedImage.Url = facebookPictureUrl;

    Console.WriteLine($"Saving downloaded facebook image to cache file");
    File.WriteAllText(Constants.FacebookImageCacheFileName, JsonConvert.SerializeObject(_cachedImage, Formatting.Indented));

    return _cachedImage.Bytes;
  }

  private static async Task<byte[]> DownloadFacebookImageAsync(string url)
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
