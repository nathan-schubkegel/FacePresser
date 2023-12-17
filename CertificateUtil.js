const Constants = require('./Constants.js').Constants;
const fs = require('fs');
const os = require('os');
const { execSync } = require('child_process');


class CertificateUtil {}

CertificateUtil.MakeCertRSA = () =>
{
  if (fs.existsSync(Constants.FacebookLoginRedirectCertFilePath)) {
    console.log("Using already-existing cert " + Constants.FacebookLoginRedirectCertFilePath);
    return;
  }
  
  console.log("Creating new cert " + Constants.FacebookLoginRedirectCertFilePath);
  
  if (os.platform() !== 'win32')
  {
    // use openssl to create new certificate + key
    let command = `openssl req -x509 -days 3650 -newkey rsa:4096 ` + 
      `-keyout "${Constants.FacebookLoginRedirectCertFilePath}.key" ` +
      `-out "${Constants.FacebookLoginRedirectCertFilePath}.pem" ` +
      `-passout "pass:${Constants.FacebookLoginRedirectCertPassword}" ` +
      `-subj "/C=US/ST=Nebrahoma/O=Whoever/CN=FacePresserCert"`;
    console.log(command);
    let stdout = execSync(command);
    console.log(stdout.toString());
    
    // use openssl to combine them into *.pfx
    command = `openssl pkcs12 -export -inkey "${Constants.FacebookLoginRedirectCertFilePath}.key" ` +
      `-in "${Constants.FacebookLoginRedirectCertFilePath}.pem" ` +
      `-out "${Constants.FacebookLoginRedirectCertFilePath}" ` +
      `-passin "pass:${Constants.FacebookLoginRedirectCertPassword}" ` +
      `-passout "pass:${Constants.FacebookLoginRedirectCertPassword}"`;
    console.log(command);
    stdout = execSync(command);
    console.log(stdout.toString());
  }
  else // windows
  {
    // use dotnet.exe to create a self-signed pfx file
    let command = `dotnet dev-certs https --export-path ${Constants.FacebookLoginRedirectCertFilePath} --trust --password "${Constants.FacebookLoginRedirectCertPassword}"`;
    console.log(command);
    let stdout = execSync(command);
    console.log(stdout.toString());
  }
  
  if (!fs.existsSync(Constants.FacebookLoginRedirectCertFilePath)) {
    throw "Failed to create cert " + Constants.FacebookLoginRedirectCertFilePath;
  }

  const fullCertFilePath = Constants.FacebookLoginRedirectCertFilePath;
  console.log("Hey. We just made a new self-signed cert at " + fullCertFilePath +
    " and you're going to need to add this new cert to FireFox, like" +
    " Tools > Options > Advanced > Certificates: View Certificates. Or" +
    " suffer the wrath of the 'oh noes this is insecure' page in your browser." +
    " For more info see https://support.mozilla.org/en-US/questions/1059377");
};

module.exports = {
   CertificateUtil: CertificateUtil
}