using Newtonsoft.Json.Linq;

public static class UserAccessTokenService
{
  public static string GetUserAccessToken()
  {
    if (File.Exists(Constants.FacebookUserAccessTokenFileName))
    {
      Console.WriteLine("Loading user access token from file " + Constants.FacebookUserAccessTokenFileName);
      return File.ReadAllText(Constants.FacebookUserAccessTokenFileName);
    }
    
    var loginNookie = Guid.NewGuid().ToString();
    var loginRedirect = $"https://localhost:{Constants.FacebookLoginRedirectListenerPort}/login_success";
    var loginUrl = $"https://www.facebook.com/v18.0/dialog/oauth?" + 
      $"client_id={Constants.FacebookAppId}&" +
      $"redirect_uri={loginRedirect}&" +
      $"state={loginNookie}&" +
      $"scope=pages_read_engagement,pages_read_user_content,pages_show_list&" +
      $"response_type=code";

    TaskCompletionSource<string> userAccessToken = new TaskCompletionSource<string>();
    FacebookLoginRedirectListener.HttpRequestDelegate d = (string url, string[] parameters) =>
    {
      Console.WriteLine("OnHttpRequest for url="+url+" and parameters="+string.Join("&", parameters));
      if (url.Contains("/login_success?") && parameters.Contains("state=" + loginNookie))
      {
        try
        {
          var p = parameters.FirstOrDefault(x => x.StartsWith("code="));
          if (p == null)
          {
            p = parameters.FirstOrDefault(x => x.StartsWith("error="));
            userAccessToken.SetException(new Exception(p ?? "facebook login probably denied"));
          }
          else
          {
            var code = p.Substring("code=".Length);

            Console.WriteLine("Received code=" + code + "! Asking facebook to decrypt it!");
            using var client = new HttpClient();
            try
            {
              var response = client.GetAsync("https://graph.facebook.com/v18.0/oauth/access_token?" +
                $"client_id={Constants.FacebookAppId}&" +
                $"redirect_uri={loginRedirect}&" +
                $"client_secret={Constants.FacebookAppSecret}&" +
                $"code={code}").Result;
              string result = response.Content.ReadAsStringAsync().Result;
              Console.WriteLine("Response from facebook: " + result);
              if (response.IsSuccessStatusCode)
              {
                // it's gonna look like
                // {
                //   "access_token": {access-token}, 
                //   "token_type": {type},
                //   "expires_in":  {seconds-til-expiration}
                // }
                var jsonRes = JObject.Parse(result);
                var token = (string)jsonRes["access_token"];
                if (token != null)
                {
                  File.WriteAllText(Constants.FacebookUserAccessTokenFileName, token);
                  userAccessToken.SetResult(token);
                }
                else
                {
                  userAccessToken.SetException(new Exception("facebook code decryption failed"));
                }
              }
              else
              {
                Console.WriteLine($"facebook returned error status code {(int)response.StatusCode} ({response.StatusCode})");
                userAccessToken.SetException(new Exception("facebook code decryption failed"));
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine("Failed to decrypt code: " + ex);
              userAccessToken.SetException(ex);
            }
          }
        }
        finally
        {
          userAccessToken.TrySetException(new Exception("whut"));
        }
      }
    };
    using var redirectListener = new FacebookLoginRedirectListener();
    redirectListener.OnHttpRequest += d;
    try
    {
      _ = redirectListener.Run();

      Console.WriteLine("Launching a firefox window and waiting up to 5 minutes for the user give app permission in facebook...");
      using (var p = System.Diagnostics.Process.Start(Constants.BrowserExePath, string.Format(Constants.BrowserExeArgs, loginUrl))) { }
      Task.WaitAny(Task.Delay(TimeSpan.FromMinutes(5)), userAccessToken.Task);
      if (!userAccessToken.Task.IsCompleted)
      {
        throw new Exception("Timed out after 5 minutes of waiting for user to give app permission in facebook");
      }
    }
    finally
    {
      redirectListener.OnHttpRequest -= d;
    }
    var accessToken = userAccessToken.Task.Result;
    Console.WriteLine("h'okay, we have user access token " + accessToken);
    return accessToken;
  }
  
  public static void DeleteCachedUserAccessToken()
  {
    if (File.Exists(Constants.FacebookUserAccessTokenFileName))
    {
      Console.WriteLine("Deleting user access token from file " + Constants.FacebookUserAccessTokenFileName);
      File.Delete(Constants.FacebookUserAccessTokenFileName);
    }
  }
}