//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web.Script.Serialization;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebServiceUnitTest
    {
        private const string AddressBase = "http://localhost:9000";
        private const string AddressInterface = "scim";
        private const string AddressRelativeGroup = WebServiceUnitTest.AddressRelativeGroups + "/";
        private const string AddressRelativeGroupTemplate = 
            WebServiceUnitTest.AddressRelativeGroups + "/{0}?" + 
            QueryKeys.ExcludedAttributes + "=" + 
            AttributeNames.Members;
        private const string AddressRelativeGroups = WebServiceUnitTest.AddressInterface + "/" + WebServiceUnitTest.PathGroups;
        private const string AddressRelativeUser = WebServiceUnitTest.AddressRelativeUsers + "/";
        private const string AddressRelativeUsers = WebServiceUnitTest.AddressInterface + "/" + WebServiceUnitTest.PathUsers;        

        private const string ContentTypeJson = "application/json";

        public const string FileNameMicrosoftIdentityModelActiveDirectory = "Microsoft.IdentityModel.Clients.ActiveDirectory.dll";
        public const string FileNameMicrosoftOwin = "Microsoft.Owin.dll";
        public const string FileNameMicrosoftOwinHostHttpListener = "Microsoft.Owin.Host.HttpListener.dll";
        public const string FileNameMicrosoftOwinHosting = "Microsoft.Owin.Hosting.dll";
        public const string FileNameMicrosoftOwinSecurity = "Microsoft.Owin.Security.dll";
        public const string FileNameMicrosoftOwinSecurityActiveDirectory = "Microsoft.Owin.Security.ActiveDirectory.dll";
        public const string FileNameMicrosoftOwinSecurityJavaScriptWebTokens = "Microsoft.Owin.Security.Jwt.dll";
        public const string FileNameMicrosoftOwinSecurityOpenStandardForAuthorization = "Microsoft.Owin.Security.OAuth.dll";
        public const string FileNameNewtonsoft = "Newtonsoft.Json.dll";
        public const string FileNameOwin = "Owin.dll";
        public const string FileNameSystemIdentityModelTokensJavaScriptWebTokens = "System.IdentityModel.Tokens.Jwt.dll";
        public const string FileNameSystemNetHypertextMarkupLanguageFormatting = "System.Net.Http.Formatting.dll";
        public const string FileNameSystemWeb = "System.Web.dll";
        public const string FileNameSystemWebExtensions = "System.Web.Extensions.dll";
        public const string FileNameSystemWebHypertextMarkupLanguage = "System.Web.Http.dll";
        public const string FileNameSystemWebOwin = "System.Web.Http.Owin.dll";

        private const string MethodDelete = "DELETE";

        private const string PathGroups = "Groups";
        private const string PathUsers = "Users";

        private static readonly Lazy<JavaScriptSerializer> Serializer =
            new Lazy<JavaScriptSerializer>(
                () =>
                    new JavaScriptSerializer());

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.WebService)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftIdentityModelActiveDirectory)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwin)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHostHttpListener)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHosting)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurity)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityActiveDirectory)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityJavaScriptWebTokens)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityOpenStandardForAuthorization)]
        [DeploymentItem(WebServiceUnitTest.FileNameNewtonsoft)]
        [DeploymentItem(WebServiceUnitTest.FileNameOwin)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemIdentityModelTokensJavaScriptWebTokens)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemNetHypertextMarkupLanguageFormatting)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWeb)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebExtensions)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebHypertextMarkupLanguage)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebOwin)]
        public void TestCreateUser()
        {
            Uri addressBase = new Uri(WebServiceUnitTest.AddressBase);

            IMonitor monitor = new ConsoleMonitor();

            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            IFileProvider provider = null;
            try
            {
                provider = new FileProvider(fileName);
                Service webService = null;
                try
                {
                    webService = new WebService(monitor, provider);
                    webService.Start(addressBase);

                    IDictionary<string, object> json = ProviderTestTemplate<FileProvider>.ComposeUserResource().ToJson();
                    string characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                    byte[] bytes = Encoding.UTF8.GetBytes(characters);

                    Uri resource = new Uri(addressBase, WebServiceUnitTest.AddressRelativeUsers);

                    WebClient client = null;
                    try
                    {
                        client = new WebClient();
                        client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                        byte[] response = client.UploadData(resource.AbsoluteUri, WebRequestMethods.Http.Post, bytes);
                        string responseCharacters = Encoding.UTF8.GetString(response);
                        IReadOnlyDictionary<string, object> responseJson =
                            WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                        Core2EnterpriseUser user = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
                        Assert.IsNotNull(user);
                        Assert.IsNotNull(
                            user
                            .Schemas
                            .SingleOrDefault(
                                (string item) =>
                                    string.Equals(
                                        SchemaIdentifiers.Core2EnterpriseUser,
                                        item,
                                        StringComparison.Ordinal)));
                        Assert.IsFalse(string.IsNullOrWhiteSpace(user.Identifier));
                        Assert.IsNotNull(user.Metadata);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(user.Metadata.ResourceType));

                        string resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeUser, user.Identifier);
                        resource = new Uri(addressBase, resourcePath);
                        bytes = new byte[0];
                        client.UploadData(resource, WebServiceUnitTest.MethodDelete, bytes);
                    }
                    finally
                    {
                        if (client != null)
                        {
                            client.Dispose();
                            client = null;
                        }
                    }
                }
                finally
                {
                    if (webService != null)
                    {
                        webService.Dispose();
                        webService = null;
                    }
                }
            }
            finally
            {
                if (provider != null)
                {
                    provider.Dispose();
                    provider = null;
                }
            }
            
        }

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.WebService)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftIdentityModelActiveDirectory)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwin)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHostHttpListener)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHosting)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurity)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityActiveDirectory)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityJavaScriptWebTokens)]
        [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityOpenStandardForAuthorization)]
        [DeploymentItem(WebServiceUnitTest.FileNameNewtonsoft)]
        [DeploymentItem(WebServiceUnitTest.FileNameOwin)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemIdentityModelTokensJavaScriptWebTokens)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemNetHypertextMarkupLanguageFormatting)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWeb)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebExtensions)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebHypertextMarkupLanguage)]
        [DeploymentItem(WebServiceUnitTest.FileNameSystemWebOwin)]
        public void TestRetrieveGroup()
        {
            Uri addressBase = new Uri(WebServiceUnitTest.AddressBase);

            IMonitor monitor = new ConsoleMonitor();

            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            IFileProvider provider = null;
            try
            {
                provider = new FileProvider(fileName);
                Service webService = null;
                try
                {
                    webService = new WebService(monitor, provider);
                    webService.Start(addressBase);

                    Guid groupIdentifier = Guid.NewGuid();
                    string resourceRelative =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            WebServiceUnitTest.AddressRelativeGroupTemplate,
                            groupIdentifier);
                    Uri resource = new Uri(addressBase, resourceRelative);

                    HttpWebResponse response = null;

                    WebClient client = null;
                    try
                    {
                        client = new WebClient();
                        try
                        {
                            client.DownloadData(resource);
                        }
                        catch (WebException exception)
                        {
                            response = exception.Response as HttpWebResponse;
                        }
                    }
                    finally
                    {
                        if (client != null)
                        {
                            client.Dispose();
                            client = null;
                        }
                    }

                    Assert.IsNotNull(response);
                    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
                }
                finally
                {
                    if (webService != null)
                    {
                        webService.Dispose();
                        webService = null;
                    }
                }
            }
            finally
            {
                if (provider != null)
                {
                    provider.Dispose();
                    provider = null;
                }
            }

        }
    }
}