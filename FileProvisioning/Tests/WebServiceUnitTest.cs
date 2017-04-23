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
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftIdentityModelActiveDirectory)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwin)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHostHttpListener)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinHosting)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurity)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityActiveDirectory)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityJavaScriptWebTokens)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftOwinSecurityOpenStandardForAuthorization)]
    [DeploymentItem(WebServiceUnitTest.FileNameMicrosoftVisualStudioValidation)]
    [DeploymentItem(WebServiceUnitTest.FileNameNewtonsoft)]
    [DeploymentItem(WebServiceUnitTest.FileNameOwin)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemIdentityModelTokensJavaScriptWebTokens)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemNetHypertextMarkupLanguageFormatting)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemWeb)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemWebExtensions)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemWebHypertextMarkupLanguage)]
    [DeploymentItem(WebServiceUnitTest.FileNameSystemWebOwin)]
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

        private const string FileNameMicrosoftIdentityModelActiveDirectory = "Microsoft.IdentityModel.Clients.ActiveDirectory.dll";
        private const string FileNameMicrosoftOwin = "Microsoft.Owin.dll";
        private const string FileNameMicrosoftOwinHostHttpListener = "Microsoft.Owin.Host.HttpListener.dll";
        private const string FileNameMicrosoftOwinHosting = "Microsoft.Owin.Hosting.dll";
        private const string FileNameMicrosoftOwinSecurity = "Microsoft.Owin.Security.dll";
        private const string FileNameMicrosoftOwinSecurityActiveDirectory = "Microsoft.Owin.Security.ActiveDirectory.dll";
        private const string FileNameMicrosoftOwinSecurityJavaScriptWebTokens = "Microsoft.Owin.Security.Jwt.dll";
        private const string FileNameMicrosoftOwinSecurityOpenStandardForAuthorization = "Microsoft.Owin.Security.OAuth.dll";
        private const string FileNameMicrosoftVisualStudioValidation = "Microsoft.VisualStudio.Validation.dll";
        private const string FileNameNewtonsoft = "Newtonsoft.Json.dll";
        private const string FileNameOwin = "Owin.dll";
        private const string FileNameSystemIdentityModelTokensJavaScriptWebTokens = "System.IdentityModel.Tokens.Jwt.dll";
        private const string FileNameSystemNetHypertextMarkupLanguageFormatting = "System.Net.Http.Formatting.dll";
        private const string FileNameSystemWeb = "System.Web.dll";
        private const string FileNameSystemWebExtensions = "System.Web.Extensions.dll";
        private const string FileNameSystemWebHypertextMarkupLanguage = "System.Web.Http.dll";
        private const string FileNameSystemWebOwin = "System.Web.Http.Owin.dll";

        private const string MethodDelete = "DELETE";

        private const string PathGroups = "Groups";
        private const string PathUsers = "Users";

        private static readonly Lazy<JavaScriptSerializer> Serializer =
            new Lazy<JavaScriptSerializer>(
                () =>
                    new JavaScriptSerializer());

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "None")]
        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.WebService)]
        public void TestCreateUser()
        {
            Uri resourceBase = new Uri(WebServiceUnitTest.AddressBase);
            Uri resourceUsers = new Uri(WebServiceUnitTest.AddressRelativeUsers, UriKind.Relative);

            IMonitor monitor = new ConsoleMonitor();
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            FileProviderBase provider = null;
            try
            {
                provider = new AccessConnectivityEngineFileProviderFactory(fileName, monitor).CreateProvider();
                Service webService = null;
                try
                {
                    webService = new WebService(monitor, provider);
                    webService.Start(resourceBase);

                    IDictionary<string, object> json = SampleComposer.Instance.ComposeUserResource().ToJson();
                    string characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                    byte[] bytes = Encoding.UTF8.GetBytes(characters);

                    Uri resource = new Uri(resourceBase, resourceUsers);

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

                        string resourcePathValue = 
                            string.Concat(WebServiceUnitTest.AddressRelativeUser, user.Identifier);
                        Uri resourcePath = new Uri(resourcePathValue, UriKind.Relative);
                        resource = new Uri(resourceBase, resourcePath);
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
        public void TestRetrieveGroup()
        {
            Uri resourceBase = new Uri(WebServiceUnitTest.AddressBase);

            IMonitor monitor = new ConsoleMonitor();

            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            FileProviderBase provider = null;
            try
            {
                provider = new AccessConnectivityEngineFileProviderFactory(fileName, monitor).CreateProvider();
                Service webService = null;
                try
                {
                    webService = new WebService(monitor, provider);
                    webService.Start(resourceBase);

                    Guid groupIdentifier = Guid.NewGuid();
                    string resourceRelativeValue =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            WebServiceUnitTest.AddressRelativeGroupTemplate,
                            groupIdentifier);
                    Uri resourceRelative = new Uri(resourceRelativeValue, UriKind.Relative);
                    Uri resource = new Uri(resourceBase, resourceRelative);

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