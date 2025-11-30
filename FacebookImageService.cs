using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public static class FacebookImageService
{
  public static async Task<byte[]> DownloadFacebookImageAsync(string url)
  {
    Console.WriteLine("Downloading image from facebook: " + url);
    using var pictureStream = new MemoryStream();
    using var client = new HttpClient();
    using HttpResponseMessage response = await client.GetAsync(url);
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
      throw new Exception(
        $"Picture download request failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}"
      );
    }
  }
}
