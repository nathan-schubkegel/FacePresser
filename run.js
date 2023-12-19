const Constants = require('./Constants.js').Constants;
const CertificateUtil = require('./CertificateUtil.js').CertificateUtil;
const ImageTypeChecker = require('./ImageTypeChecker.js').ImageTypeChecker;

console.log(Constants.FacebookAppSecret);
CertificateUtil.MakeCertRSA();
console.log(ImageTypeChecker.GetImageMimeType(new Uint8Array([0x47, 0x49, 0x46, 0x00])));