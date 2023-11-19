using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

public static class Program
{
  public static async Task Main(string[] args)
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
          Console.WriteLine("The latest facebook post has already been uploaded to the wordpress site");
        }
        else
        {
          // first determine the new page content
          // (so if this step is going to fail, it happens before anything is uploaded!)
          DetermineNewPageMessageContent(pageContent, facebookMessage);

          // upload the new image (before uploading the content, so we can get the new image id in the content)
          if (!string.IsNullOrEmpty(facebookPictureUrl))
          {
            // only download if it's different than the last picture we downloaded from facebook
            // (again to minimize facebook queries in case they're gonna notice and limit us)
            if (lastRepostedMessage.FacebookPictureUrl != facebookPictureUrl)
            {
              var imageByte = await FacebookPageService.DownloadFacebookImageAsync(facebookPictureUrl);
              // TODO: implement this
              //await UploadWordpressImage(facebookPictureUrl);
            }
            else
            {
              Console.WriteLine("Facebook image is same as last posted image; not re-downloading it");
            }

            DetermineNewPageImageContent(pageContent, facebookPictureUrl);
          }

          // then upload the new wordpress page content
          await WordPressService.SetPageContent(pageContent);

          // record posted info to disk, so we can avoid re-posting later
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

  private static void DetermineNewPageImageContent(List<string> pageContent, string facebookPictureUrl)
  {
    bool hasFacebookImage = !string.IsNullOrEmpty(facebookPictureUrl);
    if (hasFacebookImage)
    {
      // TODO: ask wordpress for the right image id

      pageContent.AddRange(new[]{
        @"<!-- wp:image {""id"":527,""sizeSlug"":""full"",""linkDestination"":""none""} -->",
        @"<figure class=""wp-block-image size-full""><img src=""" + Constants.WordPressPageImageUrl + @""" alt="""" class=""wp-image-527""/></figure>",
        @"<!-- /wp:image -->",
        @"",
      });
    }

    Console.WriteLine("/////////////////////////////////////////////////////////");
    Console.WriteLine("/////////////////// New content: ////////////////////////");
    Console.WriteLine("/////////////////////////////////////////////////////////");
    foreach (var line in pageContent) Console.WriteLine(line);
  }
}
