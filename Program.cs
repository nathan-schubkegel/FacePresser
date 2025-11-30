using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class Program
{
  public static async Task Main(string[] args)
  {
    long loopNumber = 0;
    Random rnd = new Random();
    FacebookPagePost lastRepostedFacebookPost = null;

    //await WordPressService.GetWpJson();
    //return;

    while (true)
    {
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("");
      Console.WriteLine("Loop #" + ++loopNumber);
      try
      {
        // get latest facebook post
        var facebookPost = await FacebookPageService.GetLatestFacebookPostAsync();
        if (facebookPost == null)
        {
          Console.WriteLine("Done - no facebook post found");
          goto sleepy_time;
        }

        // do nothing if it's the same as last time we checked
        if (
          lastRepostedFacebookPost != null
          && lastRepostedFacebookPost.Id == facebookPost.Id
          && lastRepostedFacebookPost.CreatedTime == facebookPost.CreatedTime
          && lastRepostedFacebookPost.UpdatedTime == facebookPost.UpdatedTime
        )
        {
          Console.WriteLine("Done - the latest facebook post hasn't changed");
          goto sleepy_time;
        }

        // get the wordpress post that was made for this facebook post, if it exists
        List<WordPressPost> candidatePosts = await WordPressService.GetWordPressPostsAfterDateTime(
          facebookPost.CreatedTime.Subtract(TimeSpan.FromSeconds(1))
        );
        Console.WriteLine(
          "Checking existing WordPress posts for one that claims to be associated with facebook post " + facebookPost.Id
        );
        WordPressPost wordpressPost = null;
        foreach (var candidatePost in candidatePosts)
        {
          if (candidatePost.FacebookPostId == facebookPost.Id)
          {
            Console.WriteLine($"Wordpress post {candidatePost.WordPressPostId} matches exactly.");
            wordpressPost = candidatePost;
            break;
          }
          else
          {
            Console.WriteLine(
              "Wordpress post {candidatePost.WordPressPostId} does not match - it uses facebook post "
                + candidatePost.FacebookPostId
            );
          }
        }

        if (wordpressPost == null)
        {
          Console.WriteLine("No matching existing Wordpress post was found.");
        }
        else if (wordpressPost.FacebookPostCreatedTime != facebookPost.CreatedTime.ToString("o"))
        {
          Console.WriteLine("The matching wordpress post has different CreatedTime than the facebook post");
          Console.WriteLine("  Facebook created time =  " + facebookPost.CreatedTime.ToString("o"));
          Console.WriteLine("  Wordpress created time = " + wordpressPost.FacebookPostCreatedTime);
        }
        else if (wordpressPost.FacebookPostUpdatedTime != facebookPost.UpdatedTime.ToString("o"))
        {
          Console.WriteLine("The matching wordpress post has different UpdatedTime than the facebook post");
          Console.WriteLine("  Facebook updated time =  " + facebookPost.UpdatedTime.ToString("o"));
          Console.WriteLine("  Wordpress updated time = " + wordpressPost.FacebookPostUpdatedTime);
        }
        else
        {
          // do nothing if the wordpress post already represents this facebook post
          // (technically can happen when the application starts up)
          Console.WriteLine("Done - the latest facebook post already matches a corresponding wordpress post");
          goto sleepy_time;
        }

        // some facebook posts have an image; upload that to wordpress first
        WordPressMediaItem featuredImage = null;
        if (!string.IsNullOrEmpty(facebookPost.FullPictureUrl))
        {
          byte[] facebookImageContent = await FacebookImageService.DownloadFacebookImageAsync(
            facebookPost.FullPictureUrl
          );
          featuredImage = await WordPressService.EnsureImageIsUploaded(facebookImageContent, facebookPost.Id);
        }

        var postName = "Posted " + facebookPost.CreatedTime.ToString("MMMM d, yyyy");
        var postContent = DetermineNewWordPressPostContent(facebookPost);
        await WordPressService.CreateOrUpdatePost(
          wordpressPost?.WordPressPostId,
          postName,
          facebookPost.CreatedTime,
          postContent,
          featuredImage
        );

        lastRepostedFacebookPost = facebookPost;
      }
      catch (Exception ex)
      {
        Console.WriteLine("Unhandled " + ex.ToString());
      }

      sleepy_time:
      int minutes = rnd.Next(1, 6); // creates a number between 1 and 5
      int seconds = rnd.Next(0, 60); // creates a number between 0 and 59
      Console.WriteLine(DateTime.Now.ToString());
      Console.WriteLine($"Sleeping {minutes} minutes {seconds} seconds until next attempt...");
      Thread.Sleep(1000 * (60 * minutes + seconds));
    }
  }

  private static List<string> DetermineNewWordPressPostContent(FacebookPagePost facebookPost)
  {
    List<string> pageContent = new();
    JObject meta = new JObject
    {
      ["facebookPostId"] = facebookPost.Id,
      ["facebookPostCreatedTime"] = facebookPost.CreatedTime.ToString("o"),
      ["facebookPostUpdatedTime"] = facebookPost.UpdatedTime.ToString("o"),
    };
    pageContent.Add(@"<!-- " + meta.ToString(Formatting.None) + @" -->");

    // add the most recent post
    foreach (var line in facebookPost.Message.GetLines().Select(x => x.Trim()).Where(x => x != ""))
    {
      // Try to make URLs look like hyperlinks
      StringBuilder reconstitutedLine = new();
      var words = line.Split(' ');
      bool first = true;
      foreach (var word in words)
      {
        if (!first)
        {
          reconstitutedLine.Append(' ');
        }
        first = false;

        if (word.StartsWith("https://"))
        {
          reconstitutedLine.Append(
            $"<a href=\"{word}\" target=\"_blank\" rel=\"noreferrer noopener\">{HttpUtility.HtmlEncode(word)}</a>"
          );
        }
        else
        {
          reconstitutedLine.Append(word);
        }
      }

      pageContent.AddRange(
        new[]
        {
          @"<!-- wp:paragraph -->",
          @"<p>" + reconstitutedLine.ToString() + @"</p>",
          @"<!-- /wp:paragraph -->",
          @"",
        }
      );
    }

    // add the footer content, if it's populated
    if (!string.IsNullOrWhiteSpace(Constants.WordPressPageFooter))
    {
      pageContent.AddRange(
        new[]
        {
          @"<!-- wp:paragraph -->",
          @"<p>" + Constants.WordPressPageFooter + @"</p>",
          @"<!-- /wp:paragraph -->",
          @"",
        }
      );
    }

    // add the "last updated" disclaimer at the end
    pageContent.AddRange(
      new[]
      {
        @"<!-- wp:paragraph -->",
        @"<p>(Last updated "
          + HttpUtility.HtmlEncode(
            facebookPost.UpdatedTime.ToLocalTime().ToString("M/d/yyyy 'at' h:mmtt")
              + " "
              + TimeZoneInfo.Local.StandardName
          )
          + @")</p>",
        @"<!-- /wp:paragraph -->",
        @"",
      }
    );

    return pageContent;
  }
}
