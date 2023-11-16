using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

Random rnd = new Random();
string lastFacebookPictureUrl = null;
long loopNumber = 0;

while (true)
{
  Console.WriteLine("Loop #" + ++loopNumber);
  try
  {
    // get user access token
    var userAccessToken = UserAccessTokenService.GetUserAccessToken();
    var pageService = new FacebookPageService(userAccessToken);

    // find the pages that can be watched 
    List<FacebookPageAccount> pageAccounts;
    try
    {
      pageAccounts = await pageService.GetPageAccounts("me");
    }
    catch
    {
      UserAccessTokenService.DeleteCachedUserAccessToken();
      throw;
    }

    Console.WriteLine("Page Accounts:" + (pageAccounts.Count == 0 ? " (none)" : ""));
    foreach (var account in pageAccounts)
    {
      Console.WriteLine(JsonConvert.SerializeObject(account, Formatting.Indented));
    }
    var pageAccount = pageAccounts.FirstOrDefault(a => a.PageName == Constants.FacebookPageName);
    if (pageAccount == null)
    {
      throw new Exception($"FacebookPageName=\"{Constants.FacebookPageName}\" was specified in " + 
        $"{Constants.ConstantsFileName} but the facebook user does not have admin access to any page by that name.");
    }

    // get the most recent posts
    var posts = await pageService.GetMostRecentPostsOnPage(pageAccount.PageId, pageAccount.PageAccessToken);
    Console.WriteLine("Page Posts:" + (posts.Count == 0 ? " (none)" : ""));
    foreach (var post in posts)
    {
      Console.WriteLine(JsonConvert.SerializeObject(post, Formatting.Indented));
    }

    var pageContent = await WordPressService.GetPageContent();
    Console.WriteLine("Page Content:" + (pageContent.Count == 0 ? " (none)" : ""));
    foreach (var line in pageContent) Console.WriteLine(line);

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

    pageContent.AddRange(new []
    {
      @"<!-- wp:paragraph -->",
      @"<p class="">(Last updated " + DateTime.Now.ToString("M/d/yyyy 'at' h:mmtt") + " " + TimeZoneInfo.Local.DisplayName + @")</p>",
      @"<!-- /wp:paragraph -->",
      @"",
    });

    // add the most recent post
    foreach (var line in posts[0].Message.GetLines().Select(x => x.Trim()).Where(x => x != ""))
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

    if (!string.IsNullOrEmpty(posts[0].FullPicture))
    {
      // only download if it's different than the last picture we downloaded
      if (lastFacebookPictureUrl != posts[0].FullPicture)
      {
        // download new image from facebook
        Console.WriteLine("Asking facebook for the picture " + posts[0].FullPicture);
        using (var pictureStream = new MemoryStream())
        using (var client = new HttpClient())
        {
          HttpResponseMessage response = await client.GetAsync(posts[0].FullPicture);
          using var responseStream = await response.Content.ReadAsStreamAsync();
          responseStream.CopyTo(pictureStream);
          pictureStream.Position = 0;
          if (response.IsSuccessStatusCode)
          {
            Console.WriteLine($"facebook's response: success ({pictureStream.Length} bytes)");
            pageContent.AddRange(new[]{
              @"<!-- wp:image {""id"":527,""sizeSlug"":""full"",""linkDestination"":""none""} -->",
              @"<figure class=""wp-block-image size-full""><img src=""" + Constants.WordPressPageImageUrl + @""" alt="""" class=""wp-image-527""/></figure>",
              @"<!-- /wp:image -->",
              @"",
            });
            
            // TODO: make image file unique (at least as far as wordpress media files are concerned)
            // and upload image to wordpress
            // and learn its uploaded URL
            
            // TODO: delete other pictures this script has uploaded (THERE CAN BE ONLY ONE!)
          }
          else
          {
            using var resultReader = new StreamReader(pictureStream);
            var result = resultReader.ReadToEnd();
            Console.WriteLine("facebook's response: " + result);
            throw new Exception($"Picture download request failed with response {(int)response.StatusCode} ({response.StatusCode}) {response.ReasonPhrase}");
          }
        }

        lastFacebookPictureUrl = posts[0].FullPicture;
      }
    }

    Console.WriteLine("/////////////////////////////////////////////////////////");
    Console.WriteLine("/////////////////// New content: ////////////////////////");
    Console.WriteLine("/////////////////////////////////////////////////////////");
    foreach (var line in pageContent) Console.WriteLine(line);

    // TODO: post these changes to wordpress
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