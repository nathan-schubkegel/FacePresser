using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

Console.WriteLine("TODO: now... what to do with this data? :)");

await WordPressService.GetPageContent("291");