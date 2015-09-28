//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public abstract class ProviderTestTemplate<TProvider> where TProvider: IProvider
    {
        public const int CountMembers = 2;

        private const string Domain = "contoso.com";
        private const long FictitiousPhoneNumber = 5551234560;

        private static readonly string ElectronicMailAddressTemplate = "{0}@" + ProviderTestTemplate<TProvider>.Domain;

        // externalId eq "{0}"
        public const string FilterByExternalIdentifierExpressionTemplate =
            AttributeNames.ExternalIdentifier + " eq \"{0}\"";

        // id eq 123 and manager eq 456
        public const string FilterByReferenceExpressionTemplate =
            AttributeNames.Identifier + " eq {0} and {1} eq {2}";

        private const string FormatUniqueIdentifierCompressed = "N";

        // addresses[type eq "Work"].postalCode
        public const string PathExpressionPostalCode =
            AttributeNames.ElectronicMailAddresses +
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

        public static WindowsAzureActiveDirectoryGroup ComposeGroupResource()
        {
            string value = Guid.NewGuid().ToString(ProviderTestTemplate<TProvider>.FormatUniqueIdentifierCompressed);

            WindowsAzureActiveDirectoryGroup result = new WindowsAzureActiveDirectoryGroup();
            result.Identifier = Guid.NewGuid().ToString();
            result.ExternalIdentifier = value;

            return result;
        }

        public static PatchRequest2 ComposeReferencePatch(
            string referenceAttributeName,
            string referencedObjectUniqueIdentifier,
            OperationName operationName)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(referenceAttributeName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(referencedObjectUniqueIdentifier));

            IPath path;
            Assert.IsTrue(Path.TryParse(referenceAttributeName, out path));
            OperationValue operationValue =
                new OperationValue()
                {
                    Value = referencedObjectUniqueIdentifier
                };
            PatchOperation operation =
                new PatchOperation()
                {
                    Name = operationName,
                    Path = path
                };
            operation.AddValue(operationValue);

            PatchRequest2 result = new PatchRequest2();
            result.AddOperation(operation);
            return result;
        }

        public static PatchRequest2 ComposeUserPatch()
        {
            string value = Guid.NewGuid().ToString(ProviderTestTemplate<TProvider>.FormatUniqueIdentifierCompressed);

            IPath path;
            PatchOperation operation;
            OperationValue operationValue;

            PatchRequest2 result = new PatchRequest2();

            Assert.IsTrue(Path.TryParse(AttributeNames.Active, out path));
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

            Assert.IsTrue(Path.TryParse(AttributeNames.DisplayName, out path));
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

            Assert.IsTrue(Path.TryParse(ProviderTestTemplate<TProvider>.PathExpressionPrimaryWorkElectronicMailAddress, out path));
            string electronicMailAddressValue =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProviderTestTemplate<TProvider>.ElectronicMailAddressTemplate,
                    value);
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

            Assert.IsTrue(Path.TryParse(ProviderTestTemplate<TProvider>.PathExpressionPostalCode, out path));
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

        public static Resource ComposeUserResource()
        {
            int countValues = 4;
            IList<string> values = new List<string>(countValues);
            for (int valueIndex = 0; valueIndex < countValues; valueIndex++)
            {
                string value = Guid.NewGuid().ToString(ProviderTestTemplate<TProvider>.FormatUniqueIdentifierCompressed);
                values.Add(value);
            }

            ElectronicMailAddress electronicMailAddress = new ElectronicMailAddress();
            electronicMailAddress.ItemType = ElectronicMailAddress.Work;
            electronicMailAddress.Primary = false;
            electronicMailAddress.Value =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProviderTestTemplate<TProvider>.ElectronicMailAddressTemplate,
                    values[1]);

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
                        ProviderTestTemplate<TProvider>.ElectronicMailAddressTemplate,
                        values[2 + proxyAddressIndex]);
                proxyAddresses.Add(proxyAddress);
            }

            Core2EnterpriseUser result = new Core2EnterpriseUser();

            result.Identifier = Guid.NewGuid().ToString();
            result.ExternalIdentifier = values[0];
            result.Active = true;
            result.DisplayName = values[0];

            result.Name = new Name();
            result.Name.FamilyName = values[0];
            result.Name.GivenName = values[0];

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
            phoneNumberWork.Value = ProviderTestTemplate<TProvider>.FictitiousPhoneNumber.ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberMobile = new PhoneNumber();
            phoneNumberMobile.ItemType = PhoneNumber.Mobile;
            phoneNumberMobile.Value = (ProviderTestTemplate<TProvider>.FictitiousPhoneNumber + 1).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberFacsimile = new PhoneNumber();
            phoneNumberFacsimile.ItemType = PhoneNumber.Fax;
            phoneNumberFacsimile.Value = (ProviderTestTemplate<TProvider>.FictitiousPhoneNumber + 2).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberPager = new PhoneNumber();
            phoneNumberPager.ItemType = PhoneNumber.Pager;
            phoneNumberPager.Value = (ProviderTestTemplate<TProvider>.FictitiousPhoneNumber + 3).ToString(CultureInfo.InvariantCulture);

            result.UserName =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProviderTestTemplate<TProvider>.ElectronicMailAddressTemplate,
                    values[0]);

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

        public abstract TProvider CreateProvider();

        public virtual async Task RunTest(Func<IProvider, Task> testFunction)
        {
            Assert.IsNotNull(testFunction);

            IProvider provider = this.CreateProvider();
            await testFunction(provider);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestCreateGroup()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        IList<Member> members = new List<Member>(ProviderTestTemplate<TProvider>.CountMembers);
                        for (int memberIndex = 0; memberIndex < ProviderTestTemplate<TProvider>.CountMembers; memberIndex++)
                        {
                            Resource userResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                            userResource = await provider.Create(userResource, correlationIdentifierCreate);
                            Assert.IsNotNull(userResource);
                            Assert.IsFalse(string.IsNullOrWhiteSpace(userResource.Identifier));
                            Member member =
                                new Member()
                                {
                                    Value = userResource.Identifier
                                };
                            members.Add(member);
                        }

                        WindowsAzureActiveDirectoryGroup group = ProviderTestTemplate<TProvider>.ComposeGroupResource();
                        group.Members = members.ToArray();
                        Resource groupResource = await provider.Create(group, correlationIdentifierCreate);
                        Assert.IsNotNull(groupResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(groupResource.Identifier));

                        IResourceIdentifier groupIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.WindowsAzureActiveDirectoryGroup,
                                Identifier = groupResource.Identifier
                            };
                        IReadOnlyCollection<IResourceIdentifier> userIdentifiers =
                            members
                            .Select(
                                (Member item) =>
                                    new ResourceIdentifier()
                                    {
                                        SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                        Identifier = item.Value
                                    })
                            .ToArray();
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestCreateUser()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        Resource outputResource = await provider.Create(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));
                        IResourceIdentifier resourceIdentifier =
                                    new ResourceIdentifier()
                                    {
                                        SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                        Identifier = outputResource.Identifier
                                    };
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestDelete()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        Resource outputResource = await provider.Create(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));
                        IResourceIdentifier resourceIdentifier =
                                    new ResourceIdentifier()
                                    {
                                        SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                        Identifier = outputResource.Identifier
                                    };
                        string correlationIdentifierDelete = Guid.NewGuid().ToString();
                        await provider.Delete(resourceIdentifier, correlationIdentifierDelete);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestQuery()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Core2EnterpriseUser inputResource = ProviderTestTemplate<TProvider>.ComposeUserResource() as Core2EnterpriseUser;
                        Resource outputResource = await provider.Create(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));

                        IReadOnlyCollection<string> requestedAttributes = new string[0];
                        IReadOnlyCollection<string> excludedAttributes = new string[0];

                        IReadOnlyCollection<IFilter> filters = null;
                        string filterExpression = 
                            string.Format(
                                CultureInfo.InvariantCulture,
                                ProviderTestTemplate<TProvider>.FilterByExternalIdentifierExpressionTemplate,
                                inputResource.ExternalIdentifier);
                        Assert.IsTrue(Filter.TryParse(filterExpression, out filters));
                        IQueryParameters queryParameters =
                            new QueryParameters(
                                    SchemaIdentifiers.Core2EnterpriseUser,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        string correlationIdentifierQuery = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources = await provider.Query(queryParameters, correlationIdentifierQuery);
                        Assert.IsNotNull(resources);
                        Resource resource = resources.Single();
                        Assert.IsTrue(string.Equals(outputResource.Identifier, resource.Identifier, StringComparison.OrdinalIgnoreCase));
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestRetrieve()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        Resource outputResource = await provider.Create(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));

                        IReadOnlyCollection<string> requestedAttributes = new string[0];
                        IReadOnlyCollection<string> excludedAttributes = new string[0];

                        IResourceRetrievalParameters resourceRetrievalParameters =
                            new ResourceRetrievalParameters(
                                SchemaIdentifiers.Core2EnterpriseUser,
                                outputResource.Identifier,
                                requestedAttributes,
                                excludedAttributes);

                        string correlationIdentifierQuery = Guid.NewGuid().ToString();

                        Resource resource = await provider.Retrieve(resourceRetrievalParameters, correlationIdentifierQuery);
                        Assert.IsNotNull(resource);
                        Assert.IsTrue(string.Equals(outputResource.Identifier, resource.Identifier, StringComparison.OrdinalIgnoreCase));
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestUpdateUser()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        PatchRequest2 patchRequest = ProviderTestTemplate<TProvider>.ComposeUserPatch();

                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        Resource outputResource = await provider.Create(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));

                        IResourceIdentifier resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = outputResource.Identifier
                            };

                        IPatch patch =
                            new Patch()
                            {
                                ResourceIdentifier = resourceIdentifier,
                                PatchRequest = patchRequest
                            };

                        string correlationIdentifierUpdate = Guid.NewGuid().ToString();
                        await provider.Update(patch, correlationIdentifierUpdate);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestUpdateManager()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifier;

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource reportResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        reportResource = await provider.Create(reportResource, correlationIdentifier);
                        Assert.IsNotNull(reportResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(reportResource.Identifier));

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource managerResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        managerResource = await provider.Create(managerResource, correlationIdentifier);
                        Assert.IsNotNull(managerResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(managerResource.Identifier));

                        IPatch patch;
                        PatchRequest2 patchRequest;
                        IResourceIdentifier resourceIdentifier;

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = reportResource.Identifier
                            };

                        patchRequest =
                            ProviderTestTemplate<TProvider>.ComposeReferencePatch(
                                AttributeNames.Manager,
                                managerResource.Identifier,
                                OperationName.Replace);

                        patch =
                            new Patch()
                            {
                                ResourceIdentifier = resourceIdentifier,
                                PatchRequest = patchRequest
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Update(patch, correlationIdentifier);

                        string filter =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                ProviderTestTemplate<TProvider>.FilterByReferenceExpressionTemplate,
                                reportResource.Identifier,
                                AttributeNames.Manager,
                                managerResource.Identifier);
                        IReadOnlyCollection<IFilter> filters = null;
                        Assert.IsTrue(Filter.TryParse(filter, out filters));

                        IReadOnlyCollection<string> requestedAttributes =
                            new string[]
                            {
                                AttributeNames.Identifier
                            };
                        IReadOnlyCollection<string> excludedAttributes = new string[0];

                        IQueryParameters queryParameters =
                            new QueryParameters(
                                    SchemaIdentifiers.Core2EnterpriseUser,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        correlationIdentifier = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources;

                        resources = await provider.Query(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(1, resources.Count);
                        Assert.AreEqual(reportResource.Identifier, resources.Single().Identifier);

                        patchRequest =
                            ProviderTestTemplate<TProvider>.ComposeReferencePatch(
                                AttributeNames.Manager,
                                managerResource.Identifier,
                                OperationName.Remove);

                        patch =
                            new Patch()
                            {
                                ResourceIdentifier = resourceIdentifier,
                                PatchRequest = patchRequest
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Update(patch, correlationIdentifier);

                        resources = await provider.Query(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(0, resources.Count);

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Delete(resourceIdentifier, correlationIdentifier);

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = managerResource.Identifier
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Delete(resourceIdentifier, correlationIdentifier);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestUpdateMembers()
        {
            Func<IProvider, Task> testFunction =
                new Func<IProvider, Task>(
                    async (IProvider provider) =>
                    {
                        string correlationIdentifier;

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource groupResource = ProviderTestTemplate<TProvider>.ComposeGroupResource();
                        groupResource = await provider.Create(groupResource, correlationIdentifier);
                        Assert.IsNotNull(groupResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(groupResource.Identifier));

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource memberResource = ProviderTestTemplate<TProvider>.ComposeUserResource();
                        memberResource = await provider.Create(memberResource, correlationIdentifier);
                        Assert.IsNotNull(memberResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(memberResource.Identifier));

                        IPatch patch;
                        PatchRequest2 patchRequest;
                        IResourceIdentifier resourceIdentifier;

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.WindowsAzureActiveDirectoryGroup,
                                Identifier = groupResource.Identifier
                            };

                        patchRequest = ProviderTestTemplate<TProvider>.ComposeReferencePatch(
                            AttributeNames.Members,
                            memberResource.Identifier,
                            OperationName.Add);

                        patch =
                            new Patch()
                            {
                                ResourceIdentifier = resourceIdentifier,
                                PatchRequest = patchRequest
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Update(patch, correlationIdentifier);

                        string filter =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                ProviderTestTemplate<TProvider>.FilterByReferenceExpressionTemplate,
                                groupResource.Identifier,
                                AttributeNames.Members,
                                memberResource.Identifier);
                        IReadOnlyCollection<IFilter> filters = null;
                        Assert.IsTrue(Filter.TryParse(filter, out filters));

                        IReadOnlyCollection<string> requestedAttributes =
                            new string[]
                            {
                                AttributeNames.Identifier
                            };
                        IReadOnlyCollection<string> excludedAttributes = new string[0];

                        IQueryParameters queryParameters =
                            new QueryParameters(
                                    SchemaIdentifiers.WindowsAzureActiveDirectoryGroup,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        correlationIdentifier = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources;

                        resources = await provider.Query(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(1, resources.Count);
                        Assert.AreEqual(groupResource.Identifier, resources.Single().Identifier);

                        patchRequest =
                            ProviderTestTemplate<TProvider>.ComposeReferencePatch(
                                AttributeNames.Members,
                                memberResource.Identifier,
                                OperationName.Remove);

                        patch =
                            new Patch()
                            {
                                ResourceIdentifier = resourceIdentifier,
                                PatchRequest = patchRequest
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Update(patch, correlationIdentifier);

                        resources = await provider.Query(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(0, resources.Count);

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Delete(resourceIdentifier, correlationIdentifier);

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = memberResource.Identifier
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.Delete(resourceIdentifier, correlationIdentifier);
                    });
            await this.RunTest(testFunction);
        }
    }
}