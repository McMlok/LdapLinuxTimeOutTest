using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Reflection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

var setttings = configuration.GetSection("ldap");

var connection = new LdapConnection(
      new LdapDirectoryIdentifier(setttings.GetValue<string>("serverName"), setttings.GetValue<int>("serverPort")),
      new NetworkCredential(setttings.GetValue<string>("userName"), setttings.GetValue<string>("Password"))
      , AuthType.Basic);
connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
connection.SessionOptions.SecureSocketLayer = setttings.GetValue<bool>("useSSL");
connection.SessionOptions.ProtocolVersion = 3;
connection.Bind();
var distinguishedName = setttings.GetValue<string>("domain").Split('.').Select(name => $"dc={name}").Aggregate((a, b) => $"{a},{b}");

while (true)
{
  var filter = $"(&(objectClass=user)(sAMAccountName=testUser))";
  var searchRequest = new SearchRequest(distinguishedName, filter, SearchScope.Subtree);

  Debug.Assert(connection != null);
  var searchResponse = (SearchResponse)await Task<DirectoryResponse>.Factory.FromAsync(
      connection.BeginSendRequest!,
      connection.EndSendRequest,
      searchRequest,
      PartialResultProcessing.NoPartialResultSupport,
      null);

  Console.WriteLine($"Search completed, results count {searchResponse.Entries.Count}");

  await Task.Delay(TimeSpan.FromMinutes(16));
}
