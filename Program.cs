using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

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

if (!string.IsNullOrEmpty(posts[0].FullPicture)) {
  pageContent.AddRange(new[]{
    @"<!-- wp:image {""id"":527,""sizeSlug"":""full"",""linkDestination"":""none""} -->",
    @"<figure class=""wp-block-image size-full""><img src=""" + Constants.WordPressPageImageUrl + @""" alt="""" class=""wp-image-527""/></figure>",
    @"<!-- /wp:image -->",
    @"",
  });

  // TODO: download new image from facebook
  // TODO: only download it if not the same as currently uploaded image on wordpress
  // TODO: upload new image to wordpress
}

Console.WriteLine("/////////////////////////////////////////////////////////");
Console.WriteLine("/////////////////// New content: ////////////////////////");
Console.WriteLine("/////////////////////////////////////////////////////////");
foreach (var line in pageContent) Console.WriteLine(line);


// TODO: post these changes
