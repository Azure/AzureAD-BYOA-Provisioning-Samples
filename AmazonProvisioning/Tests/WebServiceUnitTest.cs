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

        private const string CredentialsProfileName = "Provisioning";

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
        private const string MethodPatch = "PATCH";

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
        public void TestLifecycleGroup()
        {
            Uri addressBase = new Uri(WebServiceUnitTest.AddressBase);

            IMonitor monitor = new ConsoleMonitor();

            IAmazonWebServicesIdentityAnchoringBehavior anchoringBehavior =
                new AnchoringByIdentifierBehavior();
            AmazonWebServicesProviderBase provider = new AmazonWebServicesProvider(WebServiceUnitTest.CredentialsProfileName, anchoringBehavior);
            Service webService = null;
            try
            {
                webService = new WebService(monitor, provider);
                webService.Start(addressBase);

                string identifierGroup;
                string identifierGroupExternal;
                string identifierMemberOne;
                string identifierMemberTwo;
                
                Uri resource;

                WebClient client = null;
                try
                {
                    IDictionary<string, object> json;
                    string characters;
                    byte[] bytes;
                    byte[] response;
                    string responseCharacters;
                    IReadOnlyDictionary<string, object> responseJson;
                    Core2EnterpriseUser user;
                    Member member;
                    IReadOnlyCollection<Member> members;

                    client = new WebClient();
                    
                    identifierMemberOne = Guid.NewGuid().ToString();
                    string identifierMemberOneExternal = Guid.NewGuid().ToString();
                    user =
                        new Core2EnterpriseUser()
                        {
                            Identifier = identifierMemberOne,
                            ExternalIdentifier = identifierMemberOneExternal
                        };

                    json = user.ToJson();
                    characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                    bytes = Encoding.UTF8.GetBytes(characters);
                    resource = new Uri(addressBase, WebServiceUnitTest.AddressRelativeUsers);
                    client.Headers.Clear();
                    client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                    response = client.UploadData(resource.AbsoluteUri, WebRequestMethods.Http.Post, bytes);
                    responseCharacters = Encoding.UTF8.GetString(response);
                    responseJson =
                        WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                    user = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
                    identifierMemberOne = user.Identifier;

                    try
                    {
                        member = 
                            new Member()
                            {
                                Value = identifierMemberOne
                            };
                        members =
                            new Member[]
                                {
                                    member
                                };

                        identifierGroup = Guid.NewGuid().ToString();
                        identifierGroupExternal = Guid.NewGuid().ToString();

                        WindowsAzureActiveDirectoryGroup group =
                            new WindowsAzureActiveDirectoryGroup()
                            {
                                Identifier = identifierGroup,
                                ExternalIdentifier = identifierGroupExternal,
                                Members = members
                            };

                        json = group.ToJson();
                        characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                        bytes = Encoding.UTF8.GetBytes(characters);
                        resource = new Uri(addressBase, WebServiceUnitTest.AddressRelativeGroups);
                        client.Headers.Clear();
                        client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                        response = client.UploadData(resource.AbsoluteUri, WebRequestMethods.Http.Post, bytes);
                        responseCharacters = Encoding.UTF8.GetString(response);
                        responseJson =
                            WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                        group = new WindowsAzureActiveDirectoryGroupJsonDeserializingFactory().Create(responseJson);
                        Assert.IsNotNull(group);
                        Assert.IsNotNull(
                            group
                            .Schemas
                            .SingleOrDefault(
                                (string item) =>
                                    string.Equals(
                                        SchemaIdentifiers.WindowsAzureActiveDirectoryGroup,
                                        item,
                                        StringComparison.Ordinal)));
                        Assert.IsFalse(string.IsNullOrWhiteSpace(group.Identifier));

                        string identifierGroupAmazon = group.Identifier;

                        try
                        {
                            Assert.IsNotNull(group.Metadata);
                            Assert.IsFalse(string.IsNullOrWhiteSpace(group.Metadata.ResourceType));
                            Assert.IsFalse(string.Equals(identifierGroup, identifierGroupAmazon, StringComparison.OrdinalIgnoreCase));

                            string resourcePath = 
                                string.Format(
                                    CultureInfo.InvariantCulture, 
                                    WebServiceUnitTest.AddressRelativeGroupTemplate, 
                                    identifierGroupAmazon);
                            resource = new Uri(addressBase, resourcePath);

                            response = client.DownloadData(resource);
                            responseCharacters = Encoding.UTF8.GetString(response);
                            responseJson =
                                WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                            group = new WindowsAzureActiveDirectoryGroupJsonDeserializingFactory().Create(responseJson);
                            Assert.IsNotNull(group);
                            Assert.IsNotNull(
                                group
                                .Schemas
                                .SingleOrDefault(
                                    (string item) =>
                                        string.Equals(
                                            SchemaIdentifiers.Core2Group,
                                            item,
                                            StringComparison.Ordinal)));
                            Assert.IsFalse(string.IsNullOrWhiteSpace(group.Identifier));
                            Assert.IsTrue(string.Equals(group.Identifier, identifierGroupAmazon, StringComparison.OrdinalIgnoreCase));

                            Assert.IsFalse(string.IsNullOrWhiteSpace(group.ExternalIdentifier));
                            Assert.IsTrue(string.Equals(group.ExternalIdentifier, identifierGroupExternal, StringComparison.OrdinalIgnoreCase));

                            identifierMemberTwo = Guid.NewGuid().ToString();
                            string identifierMemberTwoExternal = Guid.NewGuid().ToString();
                            user =
                                new Core2EnterpriseUser()
                                {
                                    Identifier = identifierMemberTwo,
                                    ExternalIdentifier = identifierMemberTwoExternal
                                };

                            json = user.ToJson();
                            characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                            bytes = Encoding.UTF8.GetBytes(characters);
                            resource = new Uri(addressBase, WebServiceUnitTest.AddressRelativeUsers);
                            client.Headers.Clear();
                            client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                            response = client.UploadData(resource.AbsoluteUri, WebRequestMethods.Http.Post, bytes);
                            responseCharacters = Encoding.UTF8.GetString(response);
                            responseJson =
                                WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                            user = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
                            identifierMemberTwo = user.Identifier;

                            try
                            {
                                IResourceIdentifier resourceIdentifier =
                                    new ResourceIdentifier()
                                    {
                                        Identifier = identifierGroupAmazon,
                                        SchemaIdentifier = SchemaIdentifiers.WindowsAzureActiveDirectoryGroup
                                    };

                                IPath path = Microsoft.SystemForCrossDomainIdentityManagement.Path.Create(AttributeNames.Members);

                                OperationValue operationValue;
                                PatchOperation operation;
                                IReadOnlyCollection<PatchOperation> operations;
                                PatchRequest2 patch;
                                                                
                                operationValue =
                                    new OperationValue()
                                    {
                                        Value = identifierMemberTwo
                                    };
                                operation =
                                    new PatchOperation()
                                    {
                                        Name = OperationName.Add,
                                        Path = path
                                    };
                                operations =
                                    new PatchOperation[] 
                                        { 
                                            operation 
                                        };
                                operation.AddValue(operationValue);                                
                                
                                patch = new PatchRequest2();
                                patch.AddOperation(operation);
                                json = patch.ToJson();
                                characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                                bytes = Encoding.UTF8.GetBytes(characters);
                                resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeGroup, identifierGroupAmazon);
                                resource = new Uri(addressBase, resourcePath);
                                client.Headers.Clear();
                                client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                                response = client.UploadData(resource.AbsoluteUri, WebServiceUnitTest.MethodPatch, bytes);

                                operationValue =
                                   new OperationValue()
                                   {
                                       Value = identifierMemberTwo
                                   };
                                operation =
                                    new PatchOperation()
                                    {
                                        Name = OperationName.Remove,
                                        Path = path
                                    };
                                operations =
                                    new PatchOperation[] 
                                        { 
                                            operation 
                                        };
                                operation.AddValue(operationValue);

                                patch = new PatchRequest2();
                                patch.AddOperation(operation);
                                json = patch.ToJson();
                                characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                                bytes = Encoding.UTF8.GetBytes(characters);
                                resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeGroup, identifierGroupAmazon);
                                resource = new Uri(addressBase, resourcePath);
                                client.Headers.Clear();
                                client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                                response = client.UploadData(resource.AbsoluteUri, WebServiceUnitTest.MethodPatch, bytes);

                                operationValue =
                                   new OperationValue()
                                   {
                                       Value = identifierMemberOne
                                   };
                                operation =
                                    new PatchOperation()
                                    {
                                        Name = OperationName.Remove,
                                        Path = path
                                    };
                                operations =
                                    new PatchOperation[] 
                                        { 
                                            operation 
                                        };
                                operation.AddValue(operationValue);

                                patch = new PatchRequest2();
                                patch.AddOperation(operation);
                                json = patch.ToJson();
                                characters = WebServiceUnitTest.Serializer.Value.Serialize(json);
                                bytes = Encoding.UTF8.GetBytes(characters);
                                resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeGroup, identifierGroupAmazon);
                                resource = new Uri(addressBase, resourcePath);
                                client.Headers.Clear();
                                client.Headers.Add(HttpRequestHeader.ContentType, WebServiceUnitTest.ContentTypeJson);
                                response = client.UploadData(resource.AbsoluteUri, WebServiceUnitTest.MethodPatch, bytes);
                            }
                            finally
                            {
                                resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeUser, identifierMemberTwo);
                                resource = new Uri(addressBase, resourcePath);
                                bytes = new byte[0];
                                client.UploadData(resource, WebServiceUnitTest.MethodDelete, bytes);
                            }
                        }
                        finally
                        {
                            string resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeGroup, identifierGroupAmazon);
                            resource = new Uri(addressBase, resourcePath);
                            bytes = new byte[0];
                            client.UploadData(resource, WebServiceUnitTest.MethodDelete, bytes);
                        }
                    }
                    finally
                    {
                        string resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeUser, identifierMemberOne);
                        resource = new Uri(addressBase, resourcePath);
                        bytes = new byte[0];
                        client.UploadData(resource, WebServiceUnitTest.MethodDelete, bytes);
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
        public void TestLifecycleUser()
        {
            Uri addressBase = new Uri(WebServiceUnitTest.AddressBase);

            IMonitor monitor = new ConsoleMonitor();
            
            IAmazonWebServicesIdentityAnchoringBehavior anchoringBehavior = 
                new AnchoringByIdentifierBehavior();
            AmazonWebServicesProviderBase provider = new AmazonWebServicesProvider(WebServiceUnitTest.CredentialsProfileName, anchoringBehavior);
            Service webService = null;
            try
            {
                webService = new WebService(monitor, provider);
                webService.Start(addressBase);

                string identifier = Guid.NewGuid().ToString();
                string identifierExternal = Guid.NewGuid().ToString();
                Core2EnterpriseUser user =
                    new Core2EnterpriseUser()
                    {
                        Identifier = identifier,
                        ExternalIdentifier = identifierExternal
                    };
                IDictionary<string, object> json = user.ToJson();
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
                    user = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
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

                    string identifierAmazon = user.Identifier;
                    string resourcePath = string.Concat(WebServiceUnitTest.AddressRelativeUser, identifierAmazon);
                    resource = new Uri(addressBase, resourcePath);

                    try
                    {
                        Assert.IsNotNull(user.Metadata);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(user.Metadata.ResourceType));
                        Assert.IsFalse(string.Equals(identifier, identifierAmazon, StringComparison.OrdinalIgnoreCase));

                        response = client.DownloadData(resource);
                        responseCharacters = Encoding.UTF8.GetString(response);
                        responseJson =
                            WebServiceUnitTest.Serializer.Value.Deserialize<Dictionary<string, object>>(responseCharacters);
                        user = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
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
                        Assert.IsTrue(string.Equals(user.Identifier, identifierAmazon, StringComparison.OrdinalIgnoreCase));

                        Assert.IsFalse(string.IsNullOrWhiteSpace(user.ExternalIdentifier));
                        Assert.IsTrue(string.Equals(user.ExternalIdentifier, identifierExternal, StringComparison.OrdinalIgnoreCase));
                    }
                    finally
                    {
                        bytes = new byte[0];
                        client.UploadData(resource, WebServiceUnitTest.MethodDelete, bytes);
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
    }
}