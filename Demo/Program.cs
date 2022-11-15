using System;
using DarkLink.Web.WebFinger.Client;

using var httpClient = new HttpClient();
using var webFingerClient = new WebFingerClient(httpClient);
var descriptor = await webFingerClient.GetResourceDescriptorAsync("tech.lgbt", new Uri("acct:wiiplayer2@tech.lgbt"));
Console.WriteLine(descriptor);
