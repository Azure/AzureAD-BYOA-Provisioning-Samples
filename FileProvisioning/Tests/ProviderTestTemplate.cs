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

    public abstract class ProviderTestTemplate<TProvider> where TProvider: ProviderBase
    {
        private const int CountMembers = 2;

        // externalId eq "{0}"
        private const string FilterByExternalIdentifierExpressionTemplate =
            AttributeNames.ExternalIdentifier + " eq \"{0}\"";

        // id eq 123 and manager eq 456
        private const string FilterByReferenceExpressionTemplate =
            AttributeNames.Identifier + " eq {0} and {1} eq {2}";

        public abstract TProvider CreateProvider();

        public virtual async Task RunTest(Func<ProviderBase, Task> testFunction)
        {
            Assert.IsNotNull(testFunction);

            ProviderBase provider = this.CreateProvider();
            await testFunction(provider);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestCreateGroup()
        {
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        IList<Member> members = new List<Member>(ProviderTestTemplate<TProvider>.CountMembers);
                        for (int memberIndex = 0; memberIndex < ProviderTestTemplate<TProvider>.CountMembers; memberIndex++)
                        {
                            Resource userResource = SampleComposer.Instance.ComposeUserResource();
                            userResource = await provider.CreateAsync(userResource, correlationIdentifierCreate);
                            Assert.IsNotNull(userResource);
                            Assert.IsFalse(string.IsNullOrWhiteSpace(userResource.Identifier));
                            Member member =
                                new Member()
                                {
                                    Value = userResource.Identifier
                                };
                            members.Add(member);
                        }

                        GroupBase group = SampleComposer.Instance.ComposeGroupResource();
                        group.Members = members.ToArray();
                        Resource groupResource = await provider.CreateAsync(group, correlationIdentifierCreate);
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
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = SampleComposer.Instance.ComposeUserResource();
                        Resource outputResource = await provider.CreateAsync(inputResource, correlationIdentifierCreate);
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
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = SampleComposer.Instance.ComposeUserResource();
                        Resource outputResource = await provider.CreateAsync(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));
                        IResourceIdentifier resourceIdentifier =
                                    new ResourceIdentifier()
                                    {
                                        SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                        Identifier = outputResource.Identifier
                                    };
                        string correlationIdentifierDelete = Guid.NewGuid().ToString();
                        await provider.DeleteAsync(resourceIdentifier, correlationIdentifierDelete);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestQuery()
        {
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Core2EnterpriseUser inputResource = SampleComposer.Instance.ComposeUserResource() as Core2EnterpriseUser;
                        Resource outputResource = await provider.CreateAsync(inputResource, correlationIdentifierCreate);
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
                                    ProtocolConstants.PathUsers,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        string correlationIdentifierQuery = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources = await provider.QueryAsync(queryParameters, correlationIdentifierQuery);
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
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = SampleComposer.Instance.ComposeUserResource();
                        Resource outputResource = await provider.CreateAsync(inputResource, correlationIdentifierCreate);
                        Assert.IsNotNull(outputResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(outputResource.Identifier));

                        IReadOnlyCollection<string> requestedAttributes = new string[0];
                        IReadOnlyCollection<string> excludedAttributes = new string[0];

                        IResourceRetrievalParameters resourceRetrievalParameters =
                            new ResourceRetrievalParameters(
                                SchemaIdentifiers.Core2EnterpriseUser,
                                ProtocolConstants.PathUsers,
                                outputResource.Identifier,
                                requestedAttributes,
                                excludedAttributes);

                        string correlationIdentifierQuery = Guid.NewGuid().ToString();

                        Resource resource = await provider.RetrieveAsync(resourceRetrievalParameters, correlationIdentifierQuery);
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
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        PatchRequest2Legacy patchRequest = SampleComposer.Instance.ComposeUserPatch();

                        string correlationIdentifierCreate = Guid.NewGuid().ToString();

                        Resource inputResource = SampleComposer.Instance.ComposeUserResource();
                        Resource outputResource = await provider.CreateAsync(inputResource, correlationIdentifierCreate);
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
                        await provider.UpdateAsync(patch, correlationIdentifierUpdate);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestUpdateManager()
        {
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifier;

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource reportResource = SampleComposer.Instance.ComposeUserResource();
                        reportResource = await provider.CreateAsync(reportResource, correlationIdentifier);
                        Assert.IsNotNull(reportResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(reportResource.Identifier));

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource managerResource = SampleComposer.Instance.ComposeUserResource();
                        managerResource = await provider.CreateAsync(managerResource, correlationIdentifier);
                        Assert.IsNotNull(managerResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(managerResource.Identifier));

                        IPatch patch;
                        PatchRequest2Legacy patchRequest;
                        IResourceIdentifier resourceIdentifier;

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = reportResource.Identifier
                            };

                        patchRequest =
                            SampleComposer.Instance.ComposeReferencePatch(
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
                        await provider.UpdateAsync(patch, correlationIdentifier);

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
                                    ProtocolConstants.PathUsers,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        correlationIdentifier = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources;

                        resources = await provider.QueryAsync(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(1, resources.Count);
                        Assert.AreEqual(reportResource.Identifier, resources.Single().Identifier);

                        patchRequest =
                            SampleComposer.Instance.ComposeReferencePatch(
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
                        await provider.UpdateAsync(patch, correlationIdentifier);

                        resources = await provider.QueryAsync(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(0, resources.Count);

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.DeleteAsync(resourceIdentifier, correlationIdentifier);

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = managerResource.Identifier
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.DeleteAsync(resourceIdentifier, correlationIdentifier);
                    });
            await this.RunTest(testFunction);
        }

        [TestMethod]
        [TestCategory(TestCategory.Provider)]
        [TestCategory(TestCategory.Sample)]
        public async Task TestUpdateMembers()
        {
            Func<ProviderBase, Task> testFunction =
                new Func<ProviderBase, Task>(
                    async (ProviderBase provider) =>
                    {
                        string correlationIdentifier;

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource groupResource = SampleComposer.Instance.ComposeGroupResource();
                        groupResource = await provider.CreateAsync(groupResource, correlationIdentifier);
                        Assert.IsNotNull(groupResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(groupResource.Identifier));

                        correlationIdentifier = Guid.NewGuid().ToString();
                        Resource memberResource = SampleComposer.Instance.ComposeUserResource();
                        memberResource = await provider.CreateAsync(memberResource, correlationIdentifier);
                        Assert.IsNotNull(memberResource);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(memberResource.Identifier));

                        IPatch patch;
                        PatchRequest2Legacy patchRequest;
                        IResourceIdentifier resourceIdentifier;

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.WindowsAzureActiveDirectoryGroup,
                                Identifier = groupResource.Identifier
                            };

                        patchRequest = SampleComposer.Instance.ComposeReferencePatch(
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
                        await provider.UpdateAsync(patch, correlationIdentifier);

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
                                    ProtocolConstants.PathGroups,
                                    filters,
                                    requestedAttributes,
                                    excludedAttributes);

                        correlationIdentifier = Guid.NewGuid().ToString();

                        IReadOnlyCollection<Resource> resources;

                        resources = await provider.QueryAsync(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(1, resources.Count);
                        Assert.AreEqual(groupResource.Identifier, resources.Single().Identifier);

                        patchRequest =
                            SampleComposer.Instance.ComposeReferencePatch(
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
                        await provider.UpdateAsync(patch, correlationIdentifier);

                        resources = await provider.QueryAsync(queryParameters, correlationIdentifier);
                        Assert.IsNotNull(resources);
                        Assert.AreEqual(0, resources.Count);

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.DeleteAsync(resourceIdentifier, correlationIdentifier);

                        resourceIdentifier =
                            new ResourceIdentifier()
                            {
                                SchemaIdentifier = SchemaIdentifiers.Core2EnterpriseUser,
                                Identifier = memberResource.Identifier
                            };

                        correlationIdentifier = Guid.NewGuid().ToString();
                        await provider.DeleteAsync(resourceIdentifier, correlationIdentifier);
                    });
            await this.RunTest(testFunction);
        }
    }
}