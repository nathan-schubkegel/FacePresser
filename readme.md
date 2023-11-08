FacePresser
==========

About
-----
This is a C# console app that uses Facebook's Graph API to look at the most recent posts on a public facebook page, and updates a WordPress page with that post content.

This project avoids using Facebook's C# SDK (instead uses HttpClient for all requests) because half the point of this project is to learn, understand, and demonstrate mastery of the details of Facebook's Graph API!

This project avoids using Microsoft's built-in SslStream class (instead uses BouncyCastle's implementation of TLS 1.2) because I do not want to be coupled to Window's certificate store because when it doesn't work it's a major pain.

This project doesn't have any unit tests (yet) because I have minimal spare time to work on it.

Building/Running
--------
Download and install .NET Core 7 from https://dotnet.microsoft.com/en-us/download/dotnet/7.0

`dotnet run` to compile and run the project.

You need to create a `private_resource_constants.json` file in the app's working directory containing this information:

    {
      "FacebookAppId": "12345", // log in to https://developers.facebook.com and create a business app, then look for this ID on your app dashboard
      "FacebookAppSecret": "6789", // same
      "FacebookPageName": "Your Page Name", // See https://www.facebook.com/business/help/1199464373557428?id=418112142508425
      "FacebookListenerCertName": "whatever-cert", // just pick something
      "FacebookListenerCertPassword": "", // empty means no password
      "BrowserExePath": "C:\\Program Files\\Mozilla Firefox\\firefox.exe", // or whatever browser you want
      "BrowserExeArgs": "{0}", // {0} will be replaced with a url
    }

On initial launch, this project uses a browser to request permission to use your facebook account.

This project uses Windows-platform-specific \*.pfx cert creation code... but that could be replaced if I ever need it to run on Linux.

Licensing
---------
The contents of this repo are free and unencumbered software released into the public domain under The Unlicense.

You have complete freedom to do anything you want with the software, for any purpose.

Please refer to <http://unlicense.org/>.

Third Party Libraries
---------------------
- ASP.NET Core - MIT License - see https://github.com/dotnet/aspnetcore/tree/v7.0.0
- Newtonsoft.Json - MIT License - see https://github.com/JamesNK/Newtonsoft.Json/tree/13.0.3
- BouncyCastle.Cryptography - see https://github.com/bcgit/bc-csharp/tree/release-2.2.1