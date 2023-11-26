using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

public static class Program
{
  public static async Task Main(string[] args)
  {
    //var imageBytes = await FacebookImageCachingService.DownloadFacebookImageAsync("https://scontent-sea1-1.xx.fbcdn.net/v/t39.30808-6/403689597_10161437014445879_545593422267821146_n.jpg?stp=dst-jpg_s600x600&_nc_cat=104&ccb=1-7&_nc_sid=5f2048&_nc_ohc=QqcFGjCpcekAX8MbFC-&_nc_ht=scontent-sea1-1.xx&oh=00_AfBS7_God8MeFpmAVe4CD_iEN0UyMpT5PhFVVLzy-d5ELQ&oe=65690E6E");
    //var mediaItem = await WordPressService.UploadMediaItem(imageBytes);
    //Console.WriteLine("New MediaItem Id = " + mediaItem.Id);
    //Console.WriteLine("New MediaItem Url = " + mediaItem.Url);
    
    var items = await WordPressService.FindMediaItems(Constants.WordPressPageImageNamePattern);
    foreach (var item in items) Console.WriteLine($"{item.Id} at {item.Url}");
  }
  
  public static async Task barf(string[] args)
  {
    long loopNumber = 0;
    Random rnd = new Random();
    RepostedMessage lastRepostedMessage = null;

    while (true)
    {
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("Loop #" + ++loopNumber);
      try
      {
        // get wordpress page content first, to minimize facebook queries in case this step fails repeatedly
        var pageContent = await WordPressService.GetPageContent();

        lastRepostedMessage ??= RepostedMessage.Load();

        var (facebookMessage, facebookPictureUrl) = await FacebookPageService.GetLatestFacebookPostAsync();

        if (lastRepostedMessage.IsSameAs(facebookMessage, facebookPictureUrl))
        {
          Console.WriteLine("Done - the latest facebook post has already been uploaded to the wordpress site");
        }
        else
        {
          // first determine the new page content
          // (so if this step is going to fail, it happens before anything is uploaded!)
          DetermineNewPageMessageContent(pageContent, facebookMessage);

          // Some facebook posts have an image
          if (!string.IsNullOrEmpty(facebookPictureUrl))
          {
            byte[] facebookImageContent = await FacebookImageCachingService.GetImageAsync(facebookPictureUrl);

            // TODO: turn this on
            //var (wpImageId, wpImageUrl) = await WordPressService.EnsureImageIsUploaded(facebookImageContent);
            //string wpImageId = "527";
            //string wpImageUrl = Constants.WordPressPageImageUrl;

            //DetermineNewPageImageContent(pageContent, wpImageId, wpImageUrl);
          }

          // upload the new page content to wordpress
          await WordPressService.SetPageContent(pageContent);

          // locally record info about the facebook post
          // (so the next loop iteration can avoid doing anything until facebook's latest post changes)
          lastRepostedMessage.Save(facebookMessage, facebookPictureUrl);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Unhandled " + ex.ToString());
      }

      int minutes = rnd.Next(1, 6);  // creates a number between 1 and 5
      int seconds = rnd.Next(0, 60); // creates a number between 0 and 59
      Console.WriteLine(DateTime.Now.ToString());
      Console.WriteLine($"Sleeping {minutes} minutes {seconds} seconds until next attempt...");
      Thread.Sleep(1000 * (60 * minutes + seconds));
    }
  }

  private static void DetermineNewPageMessageContent(List<string> pageContent, string facebookMessage)
  {
    var startOfContent = new List<string>
    {
      @"<!-- wp:heading -->",
      @"<h2 class="""">" + Constants.WordPressPageHeadingTextWhereReplacementStarts + @"</h2>",
      @"<!-- /wp:heading -->",
      @"",
    };

    var startIndex = -1;
    for (int i = 0; i < pageContent.Count; i++)
    {
      if (pageContent.Skip(i).Take(startOfContent.Count).SequenceEqual(startOfContent))
      {
        startIndex = i;
        break;
      }
    }
    if (startIndex == -1)
    {
      throw new Exception("Unable to find WordPressPageHeadingTextWhereReplacementStarts in wordpress page content. Was looking for: " +
        string.Join("\r\n", startOfContent));
    }
    pageContent.RemoveRange(startIndex, pageContent.Count - startIndex);

    pageContent.AddRange(startOfContent);

    pageContent.AddRange(new []
    {
      @"<!-- wp:paragraph -->",
      @"<p class="""">(Last updated " + HttpUtility.HtmlEncode(DateTime.Now.ToString("M/d/yyyy 'at' h:mmtt") + " " + TimeZoneInfo.Local.StandardName) + @")</p>",
      @"<!-- /wp:paragraph -->",
      @"",
    });

    // add the most recent post
    foreach (var line in facebookMessage.GetLines().Select(x => x.Trim()).Where(x => x != ""))
    {
      pageContent.AddRange(new[]{
        @"<!-- wp:paragraph -->",
        @"<p class="""">" + HttpUtility.HtmlEncode(line) + @"</p>",
        @"<!-- /wp:paragraph -->",
        @"",
      });
    }

    // add the footer content
    pageContent.AddRange(new[]{
      @"<!-- wp:paragraph -->",
      @"<p class="""">" + Constants.WordPressPageFooter + @"</p>",
      @"<!-- /wp:paragraph -->",
      @""});
  }

/*
  private static void DetermineNewPageImageContent(List<string> pageContent, string wpImageId, string wpImageUrl)
  {
    pageContent.AddRange(new[]{
      @"<!-- wp:image {""id"":527,""sizeSlug"":""full"",""linkDestination"":""none""} -->",
      @"<figure class=""wp-block-image size-full""><img src=""" + Constants.WordPressPageImageUrl + @""" alt="""" class=""wp-image-527""/></figure>",
      @"<!-- /wp:image -->",
      @"",
    });
  }
*/
}
