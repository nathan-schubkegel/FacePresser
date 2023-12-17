var fs = require('fs');

class Constants {}
Constants.ConstantsFileName = "private_resource_constants.json";

const jsonText = fs.readFileSync(Constants.ConstantsFileName, 'utf8');
const settings = JSON.parse(jsonText);

function GetConstant(name)
{
  if (settings[name] === undefined) {
    throw `missing ${name} from ${Constants.ConstantsFileName}`;
  }
  return `${settings[name]}`;
}

Constants.FacebookAppSecret = GetConstant("FacebookAppSecret");
Constants.FacebookAppId = GetConstant("FacebookAppId");
Constants.FacebookPageName = GetConstant("FacebookPageName");
Constants.FacebookLoginRedirectCertFilePath = GetConstant("FacebookLoginRedirectCertFilePath") || "private_resource_self_signed_cert.pfx";
Constants.FacebookLoginRedirectCertPassword = GetConstant("FacebookLoginRedirectCertPassword");
Constants.facebookLoginRedirectListeningPort = GetConstant("FacebookLoginRedirectListeningPort");
Constants.FacebookUserAccessTokenFileName = "private_resource_user_access_token.json";
Constants.FacebookImageCacheFileName = "private_resource_last_downloaded_facebook_image.json";
Constants.LastRepostedMessageFileName = "private_resource_last_reposted_message.json";
Constants.BrowserExePath = GetConstant("BrowserExePath");
Constants.BrowserExeArgs = GetConstant("BrowserExeArgs");
Constants.WordPressAuthUsername = GetConstant("WordPressAuthUsername");
Constants.WordPressAuthPassword = GetConstant("WordPressAuthPassword");
Constants.WordPressSite = GetConstant("WordPressSite");
Constants.WordPressPageId = GetConstant("WordPressPageId");
Constants.WordPressPageHeadingTextWhereReplacementStarts = GetConstant("WordPressPageHeadingTextWhereReplacementStarts");
Constants.WordPressPageFooter = GetConstant("WordPressPageFooter");
Constants.WordPressPageImageNamePattern = GetConstant("WordPressPageImageNamePattern");

// mymodule.js
module.exports = {
   Constants: Constants
}