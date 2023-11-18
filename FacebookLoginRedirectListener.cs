using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

public class FacebookLoginRedirectListener : IDisposable
{
  TcpListener _listener;
  HashSet<TcpClient> _clients = new HashSet<TcpClient>();
  X509Certificate2 _serverCert;
  
  public delegate void HttpRequestDelegate(string url, string[] parameters);
  public event HttpRequestDelegate OnHttpRequest;

  public int ListeningPort { get; }

  public FacebookLoginRedirectListener()
  {
    // make sure cert is created
    CertificateUtil.MakeCertRSA();

    // read it from file
    _serverCert = new X509Certificate2(Constants.FacebookLoginRedirectCertFilePath, Constants.FacebookLoginRedirectCertPassword, X509KeyStorageFlags.Exportable);
    
    // start listening
    _listener = new TcpListener(IPAddress.Loopback, Constants.FacebookLoginRedirectListeningPort);
    _listener.Start();
    ListeningPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
    Console.WriteLine("Listening on TCP port " + ListeningPort);
  }

  public void Dispose()
  {
    _listener.Stop();
    lock (_clients)
    {
      foreach (var client in _clients)
      {
        try { client.Dispose(); } catch { }
      }
    }
  }

  public async Task Run()
  {
    while (true)
    {
      TcpClient client = await _listener.AcceptTcpClientAsync();
      lock (_clients) _clients.Add(client);
      _ = ProcessClient(client);
    }
  }
  
  private class MyTlsServer : DefaultTlsServer
  {
    public override ProtocolVersion[] GetProtocolVersions()
    {
      return new[] { ProtocolVersion.TLSv12 };
    }
    
    private readonly X509Certificate2 _cert;

    public MyTlsServer(X509Certificate2 cert)
        : base(new BcTlsCrypto(new SecureRandom()))
    {
      _cert = cert;
      if (!_cert.HasPrivateKey)
      {
        throw new Exception("You gotta give me a cert with a private key, mang!");
      }
    }

    protected override TlsCredentialedSigner GetRsaSignerCredentials()
    {
      var certStruct = DotNetUtilities.FromX509Certificate(_cert).CertificateStructure;
      var certificate = new BcTlsCertificate(null, certStruct);
      var cert = new Certificate(new[] { certificate });

      var privateKey = _cert.GetRSAPrivateKey();
      Console.WriteLine("type of private key is " + privateKey?.GetType().FullName);
      var keyPair = DotNetUtilities.GetKeyPair(privateKey);
      var al = new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa);

      return new BcDefaultTlsCredentialedSigner(
        new TlsCryptoParameters(this.m_context),
        (BcTlsCrypto)this.Crypto,
        keyPair.Private,
        cert,
        al);
    }
  }

  private async Task ProcessClient(TcpClient client)
  {
    Console.WriteLine("Received request from " + client.Client.RemoteEndPoint);
    await Task.Yield();
    try
    {
      TlsServerProtocol proto = new TlsServerProtocol(client.GetStream());
      proto.Accept(new MyTlsServer(_serverCert));
      // Accept() blocks until the tls handshake is complete or failed

      using var reader = new StreamReader(proto.Stream);
      string messageData = await reader.ReadLineAsync();
      
      Console.WriteLine("Received: {0}", messageData);

      var match = Regex.Match(messageData, @"GET (.*?) HTTP");
      if (!match.Success) throw new Exception("Doesn't look like HTTP GET request");

      string url = match.Groups[1].Value;
      string[] parts = url.Split('?');
      string path = parts[0];
      string[] parameters = parts.Length > 1 ? parts[1].Split('&') : new string[0];

      string htmlContent = $"<html><body><table><tr><th>URL</th><td>{path}</td></tr>";
      foreach (var parameter in parameters)
      {
        string[] nameValue = parameter.Split('=');
        if (nameValue.Length == 2)
        {
          htmlContent += $"<tr><th>{nameValue[0]}</th><td>{nameValue[1]}</td></tr>";
        }
      }
      htmlContent += @"</table>
<script>
  setTimeout(function() {
      window.close()
  }, 5000);
</script>
</body></html>";

      byte[] message = System.Text.Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n{htmlContent}\r\n");
      proto.Stream.Write(message);
      proto.Stream.Flush();
      proto.Close();
      Console.WriteLine("Successfully wrote HTML response");
      
      OnHttpRequest?.Invoke(url, parameters);
    }
    catch (Exception e)
    {
      Console.WriteLine("Exception: {0}", e);
    }
    finally
    {
      lock (_clients)
      {
        _clients.Remove(client);
        client.Dispose();
      }
    }
  }
}
