using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public class CertificateUtil
{
  // this was very helpful
  // https://stackoverflow.com/questions/55140360/how-to-create-a-selfsigned-certificate-with-certificaterequest-that-uses-microso
  public static void MakeCertRSA()
  {
    if (File.Exists(Constants.FacebookLoginRedirectCertFilePath))
    {
      Console.WriteLine("Using already-existing cert " + Constants.FacebookLoginRedirectCertFilePath);
      return;
    }
    Console.WriteLine("Creating new cert " + Constants.FacebookLoginRedirectCertFilePath);

    X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN=face-presser-selfie-cert");

    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
    {
      using (var rsa = new RSACryptoServiceProvider(4096, new CspParameters(24, "Microsoft Enhanced RSA and AES Cryptographic Provider", Guid.NewGuid().ToString())))
      {
        // this means this key will be deleted on Dispose / finalization
        rsa.PersistKeyInCsp = false;

        var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using (var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650))))
        {
          File.WriteAllBytes(Constants.FacebookLoginRedirectCertFilePath, certificate.Export(X509ContentType.Pkcs12, Constants.FacebookLoginRedirectCertPassword));
        }
      }
    }
    else // linux
    {
      using var proc = Process.Start("dotnet", "dev-certs https --export-path " + Constants.FacebookLoginRedirectCertFilePath +
        " --trust --password \"" + Constants.FacebookLoginRedirectCertPassword + "\"");
      proc.WaitForExit();
      if (proc.ExitCode != 0)
      {
        throw new Exception("dotnet dev-certs failed to create or export a certificate; see previous console output");
      }
    }

    var fullCertFilePath = Path.GetFullPath(Constants.FacebookLoginRedirectCertFilePath);
    Console.WriteLine("Hey. We just made a new self-signed cert at " + fullCertFilePath +
      " and you're going to need to add this new cert to FireFox, like" +
      " Tools > Options > Advanced > Certificates: View Certificates. Or" +
      " suffer the wrath of the 'oh noes this is insecure' page in your browser." +
      " For more info see https://support.mozilla.org/en-US/questions/1059377");
  }
}
