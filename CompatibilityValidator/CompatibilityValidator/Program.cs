//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Web.Script.Serialization;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Tools.Properties;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification="None")]
    internal static class Program
    {
        private const int ArgumentIndexAuthorizationValue = 2;
        private const int ArgumentIndexBaseAddress = 1;
        private const int ArgumentIndexDomain = 0;
        private const int ArgumentIndexVersion = 3;

        private const string AuthenticationSchemeBearer = "Bearer";

        private const int CountArgumentsRequired = 4;

        private const long FictitiousPhoneNumber = 5551234560;

        private const string ElectronicMailAddressTemplate = "{0}@{1}";

        private const string FormatUniqueIdentifierCompressed = "N";

        private const string SeperatorHeaders = ": ";

        // addresses[type eq "Work"].postalCode
        public const string PathExpressionPostalCode =
            AttributeNames.Addresses +
            "[" + AttributeNames.Type +
            " eq \"" +
            ElectronicMailAddress.Work +
            "]." +
            AttributeNames.PostalCode;

        // emails[type eq "Work" and Primary eq true]
        public const string PathExpressionPrimaryWorkElectronicMailAddress =
            AttributeNames.ElectronicMailAddresses +
            "[" + AttributeNames.Type +
            " eq \"" +
            ElectronicMailAddress.Work +
            "\" and " +
            AttributeNames.Primary +
            " eq true]";

        private static readonly Lazy<JavaScriptSerializer> Serializer =
            new Lazy<JavaScriptSerializer>(
                () =>
                    new JavaScriptSerializer());

        private static Uri baseAddress;
        private static string domain;
        private static string filterAttributeGroup = AttributeNames.ExternalIdentifier;
        private static string filterAttributeUser = AttributeNames.ExternalIdentifier;
        private static bool handledException = false;
        private static string token;
        private static Version version;

        private static string Convert(HttpRequestMessage request)
        {
            string contentType =
                request.Content != null && request.Content.Headers != null ?
                    request.Content.Headers.ContentType.ToString() : string.Empty;

            string headers = Program.Flatten(request.Headers);

            string content = request.Content != null ?
                request.Content.ReadAsStringAsync().GetAwaiter().GetResult() : string.Empty;

            string result =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationRequestTemplate,
                    request.RequestUri,
                    request.Method,
                    headers,
                    contentType,
                    content);
            return result;
        }

        private static string Convert(HttpResponseMessage response)
        {
            string content = response.Content != null ?
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult() : string.Empty;

            string result =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseTemplate,
                    response.RequestMessage.RequestUri,
                    response.RequestMessage.Method,
                    response.StatusCode,
                    content);
            return result;
        }

        private static HttpRequestMessage CreateDeletionRequest(Resource resource)
        {
            HttpRequestMessage result = null;
            try
            {
                result = resource.ComposeDeleteRequest(Program.baseAddress);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static Resource CreateGroup(HttpRequestMessage request)
        {
            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response?.Content?.ReadAsStringAsync().GetAwaiter().GetResult());
            Program.Send(request, Program.ReportGroupCreationError, responseProcessingFunction);

            try
            {
                Resource result = Program.DeserializeGroup(responseCharacters);
                return result;
            }
            catch (Exception)
            {
                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);

                Program.handledException = true;

                throw;
            }
        }

        private static Resource CreateGroup(Resource member)
        {
            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateGroupCreationRequest(member));

            Resource result = null;
            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        result = Program.CreateGroup(request));

            Program.Execute(requestCreationFunction, requestProcessingFunction);
            return result;
        }

        private static HttpRequestMessage CreateGroupCreationRequest(Resource member)
        {
            Resource resource = Program.CreateGroupResource(member);
            HttpRequestMessage result = null;
            try
            {
                result = resource.ComposePostRequest(Program.baseAddress);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static HttpRequestMessage CreateGroupFilterRequest(Resource group, string filterValue)
        {

            IFilter filter =
                new Filter(
                    Program.filterAttributeGroup,
                    ComparisonOperator.Equals,
                    filterValue);
            IReadOnlyCollection<IFilter> filters = filter.ToCollection();

            IReadOnlyCollection<string> requestedAttributes = new string[0];
            IReadOnlyCollection<string> excludedAttributes =
                new string[]
                {
                    AttributeNames.Members
                };

            HttpRequestMessage result = null;
            try
            {
                result = group.ComposeGetRequest(
                    Program.baseAddress,
                    filters,
                    requestedAttributes,
                    excludedAttributes);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static PatchRequest2 CreateGroupPatch()
        {
            string value = Guid.NewGuid().ToString(Program.FormatUniqueIdentifierCompressed);

            IPath path;
            PatchOperation operation;
            OperationValue operationValue;

            PatchRequest2 result = new PatchRequest2();

            path = Path.Create(AttributeNames.DisplayName);
            operationValue =
                new OperationValue()
                {
                    Value = value
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            path = Path.Create(Program.PathExpressionPrimaryWorkElectronicMailAddress);
            string electronicMailAddressValue =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.ElectronicMailAddressTemplate,
                    value,
                    Program.domain);
            operationValue =
                new OperationValue()
                {
                    Value = electronicMailAddressValue
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            return result;
        }

        private static Resource CreateGroupResource(Resource member)
        {
            if (SpecificationVersion.VersionTwoOh == Program.version)
            {
                string value = Guid.NewGuid().ToString(Program.FormatUniqueIdentifierCompressed);
                ElectronicMailAddress electronicMailAddress = new ElectronicMailAddress();
                electronicMailAddress.ItemType = ElectronicMailAddress.Work;
                electronicMailAddress.Primary = false;
                electronicMailAddress.Value =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Program.ElectronicMailAddressTemplate,
                        value[1],
                        Program.domain);

                WindowsAzureActiveDirectoryGroup result = new WindowsAzureActiveDirectoryGroup();
                result.DisplayName = Guid.NewGuid().ToString();
                result.MailEnabled = true;
                result.ElectronicMailAddresses =
                    new ElectronicMailAddress[]
                    {
                        electronicMailAddress
                    };
                Member reference = new Member();
                reference.Value = reference.Value = member.Identifier;
                result.Members =
                    new Member[]
                    {
                        reference
                    };

                return result;
            }
            else
            {
                Core1Group result = new Core1Group();
                result.DisplayName = Guid.NewGuid().ToString();
                Member reference = new Member();
                reference.Value = reference.Value = member.Identifier;
                result.Members =
                    new Member[]
                    {
                        reference
                    };

                return result;
            }
        }

        private static HttpRequestMessage CreateGroupUpdateRequest(Resource group, Resource user)
        {
            HttpRequestMessage result = null;
            try
            {
                if (SpecificationVersion.VersionTwoOh == Program.version)
                {
                    PatchRequest2 patch = Program.CreateGroupPatch();
                    result = group.ComposePatchRequest(Program.baseAddress, patch);
                    return result;
                }
                else
                {
                    Resource updatedResource = Program.CreateGroupResource(user);
                    updatedResource.Identifier = group.Identifier;

                    IDictionary<string, object> originalJson = group.ToJson();
                    Dictionary<string, object> updatedJson = updatedResource.ToJson();
                    updatedJson[Program.filterAttributeGroup] = originalJson[Program.filterAttributeGroup];

                    updatedResource = new Core1EnterpriseUserJsonDeserializingFactory().Create(updatedJson);
                    result = updatedResource.ComposePatchRequest(Program.baseAddress);
                    return result;
                }
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static HttpRequestMessage CreateReferenceFilterRequest(
            Resource resource, 
            string referenceAttributeName, 
            string referenceAttributeValue)
        {
            IFilter filterByAnchor =
                            new Filter(
                                AttributeNames.Identifier,
                                ComparisonOperator.Equals,
                                resource.Identifier);
            filterByAnchor.AdditionalFilter =
                new Filter(
                    referenceAttributeName,
                    ComparisonOperator.Equals,
                    referenceAttributeValue);
            IReadOnlyCollection<IFilter> filters = filterByAnchor.ToCollection();

            IReadOnlyCollection<string> requestedAttributes = AttributeNames.Identifier.ToCollection();
            IReadOnlyCollection<string> excludedAttributes = Enumerable.Empty<string>().ToArray();

            HttpRequestMessage result = null;
            try
            {
                result = resource.ComposeGetRequest(
                    Program.baseAddress,
                    filters,
                    requestedAttributes,
                    excludedAttributes);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static HttpRequestMessage CreateRetrievalRequest(Resource resource)
        {
            HttpRequestMessage result = null;
            try
            {
                result = resource.ComposeGetRequest(Program.baseAddress);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static Resource CreateUser(HttpRequestMessage request)
        {
            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response?.Content?.ReadAsStringAsync().GetAwaiter().GetResult());
            Program.Send(request, Program.ReportUserCreationError, responseProcessingFunction);

            try
            {
                Resource result = Program.DeserializeUser(responseCharacters);
                return result;
            }
            catch (Exception)
            {
                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);

                Program.handledException = true;

                throw;
            }
        }

        private static Resource CreateUser(Resource manager = null)
        {
            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateUserCreationRequest(manager));

            Resource result = null;
            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        result = Program.CreateUser(request));

            Program.Execute(requestCreationFunction, requestProcessingFunction);
            return result;
        }

        private static HttpRequestMessage CreateUserCreationRequest(Resource manager = null)
        {
            Resource resource = Program.CreateUserResource(manager);
            HttpRequestMessage result = null;
            try
            {
                result = resource.ComposePostRequest(Program.baseAddress);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static HttpRequestMessage CreateUserFilterRequest(Resource user, string filterValue)
        {
            IFilter filter =
                new Filter(
                    Program.filterAttributeUser,
                    ComparisonOperator.Equals,
                    filterValue);
            IReadOnlyCollection<IFilter> filters = filter.ToCollection();

            IReadOnlyCollection<string> requestedAttributes = new string[0];
            IReadOnlyCollection<string> excludedAttributes = new string[0];
            
            HttpRequestMessage result = null;
            try
            {
                result = user.ComposeGetRequest(
                    Program.baseAddress,
                    filters,
                    requestedAttributes,
                    excludedAttributes);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }
        
        public static PatchRequest2 CreateUserPatch()
        {
            string value = Guid.NewGuid().ToString(Program.FormatUniqueIdentifierCompressed);

            IPath path;
            PatchOperation operation;
            OperationValue operationValue;

            PatchRequest2 result = new PatchRequest2();

            path = Path.Create(AttributeNames.Active);
            operationValue =
                new OperationValue()
                {
                    Value = bool.FalseString
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            path = Path.Create(AttributeNames.DisplayName);
            operationValue =
                new OperationValue()
                {
                    Value = value
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

           path = Path.Create(Program.PathExpressionPrimaryWorkElectronicMailAddress);
            string electronicMailAddressValue =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.ElectronicMailAddressTemplate,
                    value,
                    Program.domain);
            operationValue =
                new OperationValue()
                {
                    Value = electronicMailAddressValue
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            path = Path.Create(Program.PathExpressionPostalCode);
            operationValue =
                new OperationValue()
                {
                    Value = value
                };
            operation =
                new PatchOperation()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            return result;
        }

        public static Resource CreateUserResource(Resource manager = null)
        {
            int countValues = 4;
            IList<string> values = new List<string>(countValues);
            for (int valueIndex = 0; valueIndex < countValues; valueIndex++)
            {
                string value = Guid.NewGuid().ToString(Program.FormatUniqueIdentifierCompressed);
                values.Add(value);
            }

            ElectronicMailAddress electronicMailAddress = new ElectronicMailAddress();
            electronicMailAddress.ItemType = ElectronicMailAddress.Work;
            electronicMailAddress.Primary = false;
            electronicMailAddress.Value =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.ElectronicMailAddressTemplate,
                    values[1],
                    Program.domain);

            int countProxyAddresses = 2;
            IList<ElectronicMailAddress> proxyAddresses = new List<ElectronicMailAddress>(countProxyAddresses);
            for (int proxyAddressIndex = 0; proxyAddressIndex < countProxyAddresses; proxyAddressIndex++)
            {
                ElectronicMailAddress proxyAddress = new ElectronicMailAddress();
                proxyAddress.ItemType = ElectronicMailAddress.Other;
                proxyAddress.Primary = false;
                proxyAddress.Value =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Program.ElectronicMailAddressTemplate,
                        values[2 + proxyAddressIndex],
                        Program.domain);
                proxyAddresses.Add(proxyAddress);
            }
            
            Address workAddress = new Address();
            workAddress.ItemType = Address.Work;
            workAddress.StreetAddress = values[0];
            workAddress.PostalCode = values[0];

            Address officeLocation = new Address();
            officeLocation.ItemType = Address.Other;
            officeLocation.Primary = false;
            officeLocation.Formatted = values[0];

            PhoneNumber phoneNumberWork = new PhoneNumber();
            phoneNumberWork.ItemType = PhoneNumber.Work;
            phoneNumberWork.Value = Program.FictitiousPhoneNumber.ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberMobile = new PhoneNumber();
            phoneNumberMobile.ItemType = PhoneNumber.Mobile;
            phoneNumberMobile.Value = (Program.FictitiousPhoneNumber + 1).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberFacsimile = new PhoneNumber();
            phoneNumberFacsimile.ItemType = PhoneNumber.Fax;
            phoneNumberFacsimile.Value = (Program.FictitiousPhoneNumber + 2).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberPager = new PhoneNumber();
            phoneNumberPager.ItemType = PhoneNumber.Pager;
            phoneNumberPager.Value = (Program.FictitiousPhoneNumber + 3).ToString(CultureInfo.InvariantCulture);

            if (SpecificationVersion.VersionTwoOh == Program.version)
            {
                Manager managerValue;
                if (manager != null)
                {
                    managerValue = new Manager();
                    managerValue.Value = manager.Identifier;
                }
                else
                {
                    managerValue = null;
                }

                Core2EnterpriseUser result = new Core2EnterpriseUser();
                result.Identifier = Guid.NewGuid().ToString();
                result.ExternalIdentifier = values[0];
                result.Active = true;
                result.DisplayName = values[0];

                result.Name = new Name();
                result.Name.FamilyName = values[0];
                result.Name.GivenName = values[0];

                result.UserName =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.ElectronicMailAddressTemplate,
                    values[0],
                    Program.domain);

                result.Manager = managerValue;

                result.Addresses =
                    new Address[]
                    {
                        workAddress,
                        officeLocation
                    };

                result.ElectronicMailAddresses =
                    new ElectronicMailAddress[]
                    {
                        electronicMailAddress,
                    }
                        .Union(proxyAddresses)
                        .ToArray();

                result.PhoneNumbers =
                    new PhoneNumber[]
                {
                    phoneNumberWork,
                    phoneNumberFacsimile,
                    phoneNumberMobile,
                    phoneNumberPager
                };

                return result;
            }
            else
            {
                Core1EnterpriseUserManager managerValue;
                if (manager != null)
                {
                    managerValue = new Core1EnterpriseUserManager();
                    managerValue.ManagerId = manager.Identifier;
                }
                else
                {
                    managerValue = null;
                }

                Core1EnterpriseUser result = new Core1EnterpriseUser();
                result.Identifier = Guid.NewGuid().ToString();
                result.ExternalIdentifier = values[0];
                result.Active = true;
                result.DisplayName = values[0];

                result.Name = new Name();
                result.Name.FamilyName = values[0];
                result.Name.GivenName = values[0];

                result.UserName =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.ElectronicMailAddressTemplate,
                    values[0],
                    Program.domain);

                if (managerValue != null)
                {
                    result.EnterpriseExtension = new ExtensionAttributeEnterprise();
                    result.EnterpriseExtension.Manager = managerValue;
                }

                result.Addresses =
                    new Address[]
                    {
                        workAddress,
                        officeLocation
                    };

                result.ElectronicMailAddresses =
                    new ElectronicMailAddress[]
                    {
                        electronicMailAddress,
                    }
                        .Union(proxyAddresses)
                        .ToArray();

                result.PhoneNumbers =
                    new PhoneNumber[]
                        {
                            phoneNumberWork,
                            phoneNumberFacsimile,
                            phoneNumberMobile,
                            phoneNumberPager
                        };

                return result;
            }
        }

        private static HttpRequestMessage CreateUserUpdateRequest(Resource user)
        {
            HttpRequestMessage result = null;
            try
            {
                if (SpecificationVersion.VersionTwoOh == Program.version)
                {
                    PatchRequest2 patch = Program.CreateUserPatch();
                    result = user.ComposePatchRequest(Program.baseAddress, patch);
                    return result;
                }
                else
                {
                    Resource updatedResource = Program.CreateUserResource();
                    updatedResource.Identifier = user.Identifier;

                    IDictionary<string, object> originalJson = user.ToJson();
                    Dictionary<string, object> updatedJson = updatedResource.ToJson();
                    updatedJson[Program.filterAttributeUser] = originalJson[Program.filterAttributeUser];

                    updatedResource = new Core1EnterpriseUserJsonDeserializingFactory().Create(updatedJson);
                    result = updatedResource.ComposePatchRequest(Program.baseAddress);
                    return result;
                }
            }
            catch
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }

                throw;
            }
        }

        private static Resource DeserializeGroup(string characters)
        {
            IReadOnlyDictionary<string, object> responseJson =
                        Program
                        .Serializer
                        .Value
                        .Deserialize<Dictionary<string, object>>(characters);
            Resource result;
            if (SpecificationVersion.VersionTwoOh == Program.version)
            {
                result = new WindowsAzureActiveDirectoryGroupExtensionJsonDeserializingFactory().Create(responseJson);
            }
            else
            {
                result = new Core1GroupJsonDeserializingFactory().Create(responseJson);
            }
            return result;
        }

        private static Resource DeserializeUser(string characters)
        {
            IReadOnlyDictionary<string, object> responseJson =
                        Program
                        .Serializer
                        .Value
                        .Deserialize<Dictionary<string, object>>(characters);
            Resource result;
            if (SpecificationVersion.VersionTwoOh == Program.version)
            {
                result = new Core2EnterpriseUserJsonDeserializingFactory().Create(responseJson);
            }
            else
            {
                result = new Core1EnterpriseUserJsonDeserializingFactory().Create(responseJson);
            }
            return result;
        }

        private static void Execute(
            Func<HttpRequestMessage> systemForCrossDomainIdentityManagementRequestFactoryFunction,
            Action<HttpRequestMessage> requestProcessingFunction)
        {
            HttpRequestMessage request = null;
            try
            {
                request = systemForCrossDomainIdentityManagementRequestFactoryFunction();
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        Program.AuthenticationSchemeBearer,
                        Program.token);
                requestProcessingFunction(request);
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
                    request = null;
                }
            }
        }

        private static bool IsFatal(Exception exception)
        {
            return
                exception is ThreadAbortException ||
                exception is ThreadInterruptedException ||
                exception is StackOverflowException ||
                exception is OutOfMemoryException;
        }

        private static string Flatten(HttpRequestHeaders headers)
        {
            IEnumerable<string> headerStrings =
                headers
                .Select(
                    (KeyValuePair<string, IEnumerable<string>> item) =>
                        new KeyValuePair<string, string>(
                            item.Key,
                            string.Join(Environment.NewLine, item.Value)))
                .Select(
                    (KeyValuePair<string, string> item) =>
                        string.Concat(item.Key, Program.SeperatorHeaders, item.Value));
            string result = string.Join(Environment.NewLine, headerStrings);
            return result;
        }

        internal static void Main(string[] arguments)
        {
            if (Program.CountArgumentsRequired > arguments.Length)
            {
                Console.WriteLine(CompatibilityValidatorResources.InformationUsage);
                return;
            }

            string argumentBaseAddress = arguments[Program.ArgumentIndexBaseAddress];
            if (!Uri.TryCreate(argumentBaseAddress, UriKind.Absolute, out Program.baseAddress))
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidAddressTemplate,
                        argumentBaseAddress);
                Console.WriteLine(CompatibilityValidatorResources.InformationUsage);
                Console.WriteLine(exceptionMessage);
                return;
            }

            string argumentVersion = arguments[Program.ArgumentIndexVersion];
            if (!Version.TryParse(argumentVersion, out Program.version))
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidVersionTemplate,
                        argumentVersion);
                Console.WriteLine(CompatibilityValidatorResources.InformationUsage);
                Console.WriteLine(exceptionMessage);
                return;
            }

            Program.domain = arguments[Program.ArgumentIndexDomain];
            Program.token = arguments[Program.ArgumentIndexAuthorizationValue];

            bool testGroups = false;
            if (arguments.Length > Program.CountArgumentsRequired)
            {
                string argumentValue = arguments[Program.CountArgumentsRequired];
                if (!bool.TryParse(argumentValue, out testGroups))
                {
                    Program.filterAttributeUser = arguments[Program.CountArgumentsRequired];

                    int indexNextArgument = Program.CountArgumentsRequired + 1;
                    if (arguments.Length > indexNextArgument)
                    {
                        Program.filterAttributeGroup = arguments[indexNextArgument];
                    }
                }
                else
                {
                    int indexNextArgument = Program.CountArgumentsRequired + 1;
                    if (arguments.Length > indexNextArgument)
                    {
                        Program.filterAttributeUser = arguments[indexNextArgument];

                        indexNextArgument = indexNextArgument + 1;
                        if (arguments.Length > indexNextArgument)
                        {
                            Program.filterAttributeGroup = arguments[indexNextArgument];
                        }
                    }
                }
                
            }
            
            Program.TestCompatibility(testGroups);
        }

        private static void QueryGroup(Resource group)
        {
            Program.handledException = false;

            if (!string.Equals(AttributeNames.ExternalIdentifier, Program.filterAttributeGroup, StringComparison.OrdinalIgnoreCase))
            {
                string warning =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.WarningCustomGroupFilterTemplate,
                        Program.filterAttributeGroup);
                Console.WriteLine(warning);
                Console.WriteLine();
            }

            IDictionary<string, object> json = group.ToJson();
            KeyValuePair<string, object> value =
                json
                .SingleOrDefault(
                    (KeyValuePair<string, object> item) =>
                        string.Equals(Program.filterAttributeGroup, item.Key, StringComparison.OrdinalIgnoreCase));
            if (null == value.Key)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidFilteringAttributeName,
                        Program.filterAttributeGroup);
                Console.WriteLine(exceptionMessage);
                Console.WriteLine();
                return;
            }

            string filterValue = value.Value as string;
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidFilteringAttributeName,
                        Program.filterAttributeGroup);
                Console.WriteLine(exceptionMessage);
                Console.WriteLine();
                return;
            }

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateGroupFilterRequest(group, filterValue));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, Program.ReportGroupQueryingError, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeGroup(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static void QueryUser(Resource user)
        {
            Program.handledException = false;

            if (!string.Equals(AttributeNames.ExternalIdentifier, Program.filterAttributeUser, StringComparison.OrdinalIgnoreCase))
            {
                string warning =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.WarningCustomUserFilterTemplate,
                        Program.filterAttributeUser);
                Console.WriteLine(warning);
                Console.WriteLine();
            }

            IDictionary<string, object> json = user.ToJson();
            KeyValuePair<string, object> value =
                json
                .SingleOrDefault(
                    (KeyValuePair<string, object> item) =>
                        string.Equals(Program.filterAttributeUser, item.Key, StringComparison.OrdinalIgnoreCase));
            if (null == value.Key)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidFilteringAttributeName,
                        Program.filterAttributeUser);
                Console.WriteLine(exceptionMessage);
                Console.WriteLine();
                return;
            }

            string filterValue = value.Value as string;
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidFilteringAttributeName,
                        Program.filterAttributeUser);
                Console.WriteLine(exceptionMessage);
                Console.WriteLine();
                return;
            }

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateUserFilterRequest(user, filterValue));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, Program.ReportUserQueryingError, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeUser(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static bool ReportGroupCreationError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningGroupCreationFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return true;
        }

        private static bool ReportGroupDeletionError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningGroupDeletionFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportGroupQueryingError(string request, string response)
        {
            string warning =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.WarningGroupQueryFailedTemplate,
                    Program.filterAttributeUser,
                    CompatibilityValidatorResources.InformationUsage);
            Console.WriteLine(warning);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportGroupRetrievalError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningGroupRetrievalFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportGroupUpdateError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningGroupUpdateFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportReferenceFilteringError(
            string referenceAttributeName, 
            string request, 
            string response)
        {
            string warning =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.WarningReferenceQueryFailedTemplate,
                    referenceAttributeName);
            Console.WriteLine(warning);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportUserCreationError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningUserCreationFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return true;
        }

        private static bool ReportUserDeletionError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningUserDeletionFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportUserQueryingError(string request, string response)
        {
            string warning =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.WarningUserQueryFailedTemplate,
                    Program.filterAttributeUser,
                    CompatibilityValidatorResources.InformationUsage);
            Console.WriteLine(warning);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportUserRetrievalError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningUserRetrievalFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static bool ReportUserUpdateError(string request, string response)
        {
            Console.WriteLine(CompatibilityValidatorResources.WarningUserUpdateFailed);
            Console.WriteLine();
            Console.WriteLine(request);
            Console.WriteLine();
            Console.WriteLine(response);
            Console.WriteLine();
            Program.handledException = true;
            return false;
        }

        private static void RetrieveGroup(Resource group)
        {
            Program.handledException = false;

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateRetrievalRequest(group));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, Program.ReportGroupRetrievalError, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeGroup(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static void RetrieveUser(Resource user)
        {
            Program.handledException = false;

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateRetrievalRequest(user));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, Program.ReportUserRetrievalError, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeUser(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static void Send(
            HttpRequestMessage request,
            Func<string, string, bool> errorHandler,
            Action<HttpResponseMessage> responseProcessingFunction = null)
        {
            string serializedRequest = Program.Convert(request);
            HttpResponseMessage response = null;
            try
            {
                HttpClient client = null;
                try
                {
                    client = new HttpClient();
                    response = client.SendAsync(request).GetAwaiter().GetResult();
                    string serializedResponse = Program.Convert(response);
                    HttpResponseMessage duplicateReference = null;
                    try
                    {
                        try
                        {
                            duplicateReference = response.EnsureSuccessStatusCode();
                            duplicateReference = null;
                        }
                        catch
                        {
                            if (errorHandler(serializedRequest, serializedResponse))
                            {
                                throw;
                            }
                        }                       
                    }
                    finally
                    {
                        if (duplicateReference != null)
                        {
                            duplicateReference.Dispose();
                            duplicateReference = null;
                        }
                    }
                    if (responseProcessingFunction != null)
                    {
                        responseProcessingFunction(response);
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
                if (response != null)
                {
                    response.Dispose();
                    response = null;
                }
            }
        }

        private static void TestCompatibility(bool testGroups)
        {
            bool continueTest = false;

            try
            {
                Resource user = Program.CreateUser();
                try
                {
                    try
                    {
                        Program.QueryUser(user);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        string warning =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CompatibilityValidatorResources.WarningUserQueryFailedTemplate,
                                Program.filterAttributeUser,
                                CompatibilityValidatorResources.InformationUsage);
                        Console.WriteLine(warning);
                        Console.WriteLine();
                    }

                    try
                    {
                        Program.RetrieveUser(user);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningUserRetrievalFailed);
                        Console.WriteLine();
                    }

                    if (testGroups)
                    {
                        Program.TestGroupCompatibility(user);
                    }
                    else
                    {
                        Console.WriteLine(CompatibilityValidatorResources.WarningGroupTestSkipped);
                    }

                    try
                    {
                        Program.UpdateUser(user);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningUserUpdateFailed);
                        Console.WriteLine();
                    }
                }
                finally
                {
                    continueTest = Program.TryDeleteUser(user);
                }
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                if (Program.handledException)
                {
                    return;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningUserCreationFailed);
                Console.WriteLine();

                return;
            }

            if (!continueTest)
            {
                return;
            }

            Resource manager;
            try
            {
                manager = Program.CreateUser();
                try
                {
                    Resource subordinate = Program.CreateUser(manager);
                    try
                    {
                        Program.TestManagerReference(subordinate);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningUserRetrievalFailed);
                        Console.WriteLine();
                    }
                    finally
                    {
                        Program.TryDeleteUser(subordinate);
                    }
                }
                finally
                {
                    Program.TryDeleteUser(manager);
                }
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                if (Program.handledException)
                {
                    return;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningUserCreationFailed);
                Console.WriteLine();

                return;
            }
        }

        private static void TestGroupCompatibility(Resource user)
        {
            try
            {
                Resource group = Program.CreateGroup(user);
                try
                {
                    try
                    {
                        Program.QueryGroup(group);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        string warning =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CompatibilityValidatorResources.WarningGroupQueryFailedTemplate,
                                Program.filterAttributeUser,
                                CompatibilityValidatorResources.InformationUsage);
                        Console.WriteLine(warning);
                        Console.WriteLine();
                    }

                    try
                    {
                        Program.RetrieveGroup(group);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningGroupRetrievalFailed);
                        Console.WriteLine();
                    }

                    try
                    {
                        Program.TestMembershipReference(group, user);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningGroupRetrievalFailed);
                        Console.WriteLine();
                    }

                    try
                    {
                        Program.UpdateGroup(group, user);
                    }
                    catch (Exception exception)
                    {
                        if (Program.IsFatal(exception))
                        {
                            throw;
                        }

                        if (Program.handledException)
                        {
                            return;
                        }

                        Console.WriteLine(CompatibilityValidatorResources.WarningGroupUpdateFailed);
                        Console.WriteLine();
                    }
                }
                finally
                {
                    Program.TryDeleteGroup(group);
                }
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                if (Program.handledException)
                {
                    return;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningGroupCreationFailed);
                Console.WriteLine();
            }
        }

        private static void TestManagerReference(Resource user)
        {
            Program.handledException = false;

            IDictionary<string, object> json = user.ToJson();
            KeyValuePair<string, object> value =
                json
                .SingleOrDefault(
                    (KeyValuePair<string, object> item) =>
                        string.Equals(AttributeNames.Manager, item.Key, StringComparison.OrdinalIgnoreCase));
            if (null == value.Key)
            {
                Console.WriteLine(CompatibilityValidatorResources.WarningManagerAttributeNotSupported);
                Console.WriteLine();
                return;
            }

            string filterValue = value.Value as string;
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CompatibilityValidatorResources.ExceptionInvalidManagerAttributeValueTemplate,
                        value);
                Console.WriteLine(exceptionMessage);
                Console.WriteLine();
                return;
            }

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateReferenceFilterRequest(user, AttributeNames.Manager, filterValue));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Func<string, string, bool> errorHandlingFunction =
                new Func<string, string, bool>(
                    (string request, string response) =>
                        Program.ReportReferenceFilteringError(AttributeNames.Manager, request, response));

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, errorHandlingFunction, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeUser(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static void TestMembershipReference(Resource group, Resource user)
        {
            Program.handledException = false;

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateReferenceFilterRequest(group, AttributeNames.Manager, user.Identifier));

            string responseCharacters = null;
            Action<HttpResponseMessage> responseProcessingFunction =
                new Action<HttpResponseMessage>(
                    (HttpResponseMessage response) =>
                        responseCharacters = response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Func<string, string, bool> errorHandlingFunction =
                new Func<string, string, bool>(
                    (string request, string response) =>
                        Program.ReportReferenceFilteringError(AttributeNames.Members, request, response));

            Action<HttpRequestMessage> requestProcessingFunction =
                new Action<HttpRequestMessage>(
                    (HttpRequestMessage request) =>
                        Program.Send(request, errorHandlingFunction, responseProcessingFunction));

            Program.Execute(requestCreationFunction, requestProcessingFunction);

            try
            {
                Program.DeserializeGroup(responseCharacters);
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                Console.WriteLine(CompatibilityValidatorResources.WarningDeserializationFailed);
                Console.WriteLine();
                string responseInformation =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CompatibilityValidatorResources.InformationResponseValueTemplate,
                    responseCharacters);
                Console.WriteLine(responseInformation);
            }
        }

        private static bool TryDeleteGroup(Resource group)
        {
            Program.handledException = false;

            try
            {
                Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateDeletionRequest(group));

                Action<HttpRequestMessage> requestProcessingFunction =
                     new Action<HttpRequestMessage>(
                         (HttpRequestMessage request) =>
                             Program.Send(request, Program.ReportGroupDeletionError));


                Program.Execute(requestCreationFunction, requestProcessingFunction);
                return true;
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                if (!Program.handledException)
                {
                    Console.WriteLine(CompatibilityValidatorResources.WarningGroupDeletionFailed);
                    Console.WriteLine();
                }

                return false;
            }
        }

        private static bool TryDeleteUser(Resource user)
        {
            Program.handledException = false;

            try
            {
                Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateDeletionRequest(user));

                Action<HttpRequestMessage> requestProcessingFunction =
                     new Action<HttpRequestMessage>(
                         (HttpRequestMessage request) =>
                             Program.Send(request, Program.ReportUserDeletionError));

                Program.Execute(requestCreationFunction, requestProcessingFunction);
                return true;
            }
            catch (Exception exception)
            {
                if (Program.IsFatal(exception))
                {
                    throw;
                }

                if (!Program.handledException)
                {
                    Console.WriteLine(CompatibilityValidatorResources.WarningUserDeletionFailed);
                    Console.WriteLine();
                }
                
                return false;
            }
        }

        private static void UpdateGroup(Resource group, Resource user)
        {
            Program.handledException = false;

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateGroupUpdateRequest(group, user));

            Action<HttpRequestMessage> requestProcessingFunction =
                 new Action<HttpRequestMessage>(
                     (HttpRequestMessage request) =>
                         Program.Send(request, Program.ReportGroupUpdateError));

            Program.Execute(requestCreationFunction, requestProcessingFunction);
        }

        private static void UpdateUser(Resource user)
        {
            Program.handledException = false;

            Func<HttpRequestMessage> requestCreationFunction =
                new Func<HttpRequestMessage>(
                    () =>
                        Program.CreateUserUpdateRequest(user));

            Action<HttpRequestMessage> requestProcessingFunction =
                 new Action<HttpRequestMessage>(
                     (HttpRequestMessage request) =>
                         Program.Send(request, Program.ReportUserUpdateError));

            Program.Execute(requestCreationFunction, requestProcessingFunction);
        }
    }
}