const Constants = require('./Constants.js').Constants;
const CertificateUtil = require('./CertificateUtil.js').CertificateUtil;
const ImageTypeChecker = require('./ImageTypeChecker.js').ImageTypeChecker;
const FacebookImageCachingService = require('./FacebookImageCachingService.js').FacebookImageCachingService;

async function stuff() {
  console.log(Constants.FacebookAppSecret);
  CertificateUtil.MakeCertRSA();
  console.log(ImageTypeChecker.GetImageMimeType(new Uint8Array([0x47, 0x49, 0x46, 0x00])));
  const img = await FacebookImageCachingService.DownloadFacebookImageAsync('https://www.learningcontainer.com/wp-content/uploads/2020/08/Small-Sample-png-Image-File-Download.jpg');
  console.log("downloaded image length = " + img.length);
}

stuff()
    .then(text => {
        console.log(text);
    })
    .catch(err => {
        console.log(err);
    });