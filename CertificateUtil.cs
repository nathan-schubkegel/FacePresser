using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class CertificateUtil
{
  // it creates a cert, but it's not what I needed this time...
  // holding onto this method in case I need it later
  public static void MakeCertECDSA()
  {
    var ecdsa = ECDsa.Create(); // generate asymmetric key pair
    var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
    var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(500));

    // Create PFX (PKCS #12) with private key
    File.WriteAllBytes("cert_private3.pfx", cert.Export(X509ContentType.Pfx));

    // Create Base 64 encoded CER (public key only)
    File.WriteAllText("cert_public3.cer",
      "-----BEGIN CERTIFICATE-----\r\n"
      + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
      + "\r\n-----END CERTIFICATE-----");
  }

  // this was very helpful
  // https://stackoverflow.com/questions/55140360/how-to-create-a-selfsigned-certificate-with-certificaterequest-that-uses-microso
  public static void MakeCertRSA()
  {
    if (File.Exists(Constants.FacebookListenerCertFileName))
    {
      Console.WriteLine("Using already-existing cert " + Constants.FacebookListenerCertFileName);
      return;
    }
    Console.WriteLine("Creating new cert " + Constants.FacebookListenerCertFileName);

    X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={Constants.FacebookListenerCertName}");

    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
    {
      throw new Exception("This stuff only works on windows apparently");
    }

    using (var rsa = new RSACryptoServiceProvider(4096, new CspParameters(24, "Microsoft Enhanced RSA and AES Cryptographic Provider", Guid.NewGuid().ToString())))
    {
      // this means this key will be deleted on Dispose / finalization
      rsa.PersistKeyInCsp = false;

      var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

      using (var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650))))
      {
        File.WriteAllBytes(Constants.FacebookListenerCertFileName, certificate.Export(X509ContentType.Pkcs12, Constants.FacebookListenerCertPassword));
      }
    }

    var fullCertFilePath = Path.GetFullPath(Constants.FacebookListenerCertFileName);
    Console.WriteLine("Hey. We just made a new self-signed cert at " + fullCertFilePath +
      " and you're going to need to add this new cert to FireFox, like" +
      " Tools > Options > Advanced > Certificates: View Certificates." +
      " For more info see https://support.mozilla.org/en-US/questions/1059377");
  }
}