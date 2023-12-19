const Constants = require('./Constants.js').Constants;
const fsPromises = require("fs").promises;

class FacebookImageCachingService {}

let _cachedImage = null;

// returns Promise<Uint8Array> of file contents, or async throws exception if it can't get it
FacebookImageCachingService.GetImageAsync = async (facebookPictureUrl) =>
{
  if (_cachedImage === null)
  {
    const statResult = await fsPromises.stat(Constants.FacebookImageCacheFileName);
    if (statResult)
    {
      console.log(`Loading cached image from ${Constants.FacebookImageCacheFileName}`);
      try
      {
        var jsonText = await fsPromises.readFile(Constants.FacebookImageCacheFileName, 'utf8');
        _cachedImage = JSON.parse(jsonText);
        if (!_cachedImage || !_cachedImage.bytes || !_cachedImage.bytes.length || !_cachedImage.url)
        {
          console.log(`Invalid content in ${Constants.FacebookImageCacheFileName}; will ignore file`);
          _cachedImage = {};
        }
      }
      catch (ex)
      {
        console.log(`Error reading ${Constants.FacebookImageCacheFileName}; will ignore file. Error was: ${ex}`);
        _cachedImage = {};
      }
    }
    else
    {
      console.log(`No cached image exists at ${Constants.FacebookImageCacheFileName}`);
      _cachedImage = {};
    }
  }

  if (_cachedImage.url === facebookPictureUrl)
  {
    console.log(`Found facebook image in local cache`);
    return _cachedImage.bytes;
  }

  _cachedImage.bytes = await FacebookImageCachingService.DownloadFacebookImageAsync(facebookPictureUrl);
  _cachedImage.url = facebookPictureUrl;

  console.log(`Saving downloaded facebook image to cache file`);
  const cachedImageText = JSON.stringify(_cachedImage, null, 2);
  await fsPromises.writeFile(Constants.FacebookImageCacheFileName, cachedImageText, 'utf8');

  return _cachedImage.bytes;
};

// returns Promise<Uint8Array> of file contents, or async throws exception if it can't get it
FacebookImageCachingService.DownloadFacebookImageAsync = async (url) =>
{
  console.log("Downloading image from facebook: " + url);

  const response = await fetch(url);
  if (!response.ok) {
    throw `fetch failed with status=${response.status} (${response.statusText})`;
  }
  const data = new Uint8Array(await response.arrayBuffer());
  return data;
};

module.exports = {
   FacebookImageCachingService: FacebookImageCachingService
}