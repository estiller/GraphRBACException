using System;
using System.Collections.Generic;
using Microsoft.Azure;
using Microsoft.Azure.Graph.RBAC;
using Microsoft.Azure.Graph.RBAC.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphRBACException
{
    class Program
    {
        private const string SubscriptionId = "<Place SubscriptionId Here>";
        private const string TenantId = "<Place TenantId Here>";

        private const string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";  // PowerShell well known Client ID
        private const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";          // PowerShell well known Redirect Uri

        static void Main()
        {
            string userToken = GetUserAccessToken();
            var appId = CreateApplication(userToken);
            Console.WriteLine($"Created App Id: {appId}");
        }

        static string GetUserAccessToken()
        {
            var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{TenantId}");
            var platformParams = new PlatformParameters(PromptBehavior.Always);
            var authResult = authContext.AcquireTokenAsync("https://management.core.windows.net/", ClientId, new Uri(RedirectUri), platformParams).Result;
            return authResult.AccessToken;
        }

        static string CreateApplication(string userToken)
        {
            var appInfo = new ApplicationCreateParameters
            {
                DisplayName = "TestApp",
                AvailableToOtherTenants = false,
                Homepage = "http://mydomain.com/",
                IdentifierUris = new List<string> {"http://www.mydomain.com/" + Guid.NewGuid()},
                PasswordCredentials = new List<PasswordCredential>
                {
                    new PasswordCredential
                    {
                        KeyId = Guid.NewGuid(),
                        Value = "1234",
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddYears(1)
                    }
                }
            };

            using (var client = CreateManagementClient(userToken))
            {
                try
                {
                    var app = client.Application.Create(appInfo); // Throws a Serialization Exception, but the application is created on AAD
                    return app.Application.AppId;
                }
                catch (SerializationException ex)  // This is a workaround - the application was created!
                {
                    var jsonResponse = (JObject)JsonConvert.DeserializeObject(ex.Content);
                    string appId = jsonResponse["appId"].Value<string>();
                    return appId;
                }
            }
        }

        private static GraphRbacManagementClient CreateManagementClient(string userToken)
        {
            return new GraphRbacManagementClient(TenantId, new TokenCloudCredentials(SubscriptionId, userToken));

            //return new GraphRbacManagementClient(TenantId, new TokenCloudCredentials(userToken));

            //return new GraphRbacManagementClient(TenantId, new TenantCloudCredentials
            //{
            //    TenantID = TenantId,
            //    Token = userToken
            //});
        }
    }
}
