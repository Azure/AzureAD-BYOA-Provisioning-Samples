//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Owin;

namespace Samples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Samples.Properties;
    
    public class FileProvider: FileProviderBase, IDisposable
    {
        private const string ArgumentNameColumnNames = "columnNames";
        private const string ArgumentNameCorrelationIdentifier = "correlationIdentifier";
        private const string ArgumentNameIdentifier = "identifier";
        private const string ArgumentNameParameters = "parameters";
        private const string ArgumentNamePatch = "patch";
        private const string ArgumentNameReferenceAttributeName = "referenceAttributeName";
        private const string ArgumentNameReferenceAttributeValue = "referenceAttributeValue";
        private const string ArgumentNameResource = "resource";
        private const string ArgumentNameResourceIdentifier = "resourceIdentifier";
        private const string ArgumentNameRow = "row";
        private const string ArgumentNameSchemaIdentifier = "schemaIdentifier";

        private static readonly Lazy<IReadOnlyCollection<string>> SchemaIdentifiersGroup =
            new Lazy<IReadOnlyCollection<string>>(
                () =>
                    new string[]
                    {
                        SchemaIdentifiers.Core2Group,
                        SchemaIdentifiers.WindowsAzureActiveDirectoryGroup
                    });

        private readonly IReadOnlyCollection<string> attributeNames;
        private readonly object thisLock = new object();

        private ITabularFileAdapter file;

        public FileProvider(string filePath)
            :base(filePath)
        {
            Type typeAttributeNames = typeof(AttributeNames);
            this.attributeNames =
                typeAttributeNames.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(
                    (FieldInfo item) =>
                        (string)item.GetValue(null))
                .ToArray();

            this.file = new CommaDelimitedFileAdapter(this.FilePath, attributeNames);
        }

        public override Action<IAppBuilder, HttpConfiguration> StartupBehavior
        {
            get 
            {
                return this.OnServiceStartup;
            }
        }

        public override async Task<Resource> Create(Resource resource, string correlationIdentifier)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameResource);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resource.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResource);
            }

            string informationStarting =
                string.Format(
                    CultureInfo.InvariantCulture,
                    FileProvisioningAgentResources.InformationCreating,
                    resource.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            ColumnsFactory columnsFactory;

            WindowsAzureActiveDirectoryGroup group = resource as WindowsAzureActiveDirectoryGroup;
            if (group != null)
            {
                columnsFactory = new GroupColumnsFactory(group);
            }
            else
            {
                Core2EnterpriseUser user = resource as Core2EnterpriseUser;
                if (user != null)
                {
                    columnsFactory = new UserColumnsFactory(user);
                }
                else
                {
                    string unsupportedSchema =
                        string.Join(
                            Environment.NewLine,
                            resource.Schemas);
                    throw new NotSupportedException(unsupportedSchema);
                }
            }

            IReadOnlyDictionary<string, string> columns = columnsFactory.CreateColumns();
            IRow row = await this.file.InsertRow(columns);

            ResourceFactory resourceFactory;
            if (group != null)
            {
                resourceFactory = new GroupFactory(row);
            }
            else
            {
                resourceFactory = new UserFactory(row);
            }

            Resource result = resourceFactory.CreateResource();

            if (group != null && group.Members != null && group.Members.Any())
            {
                foreach (Member member in group.Members)
                {
                    MemberColumnsFactory memberColumnsFactory = new MemberColumnsFactory(result, member);
                    IReadOnlyDictionary<string, string> memberColumns = memberColumnsFactory.CreateColumns();
                    await this.file.InsertRow(memberColumns);
                }
            }

            return result;
        }

        public override async Task Delete(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResource);
            }

            string informationStarting =
                 string.Format(
                     CultureInfo.InvariantCulture,
                     FileProvisioningAgentResources.InformationDeleting,
                     resourceIdentifier.SchemaIdentifier,
                     resourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            await this.file.RemoveRow(resourceIdentifier.Identifier);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing)
                {
                    return;
                }

                if (this.file != null)
                {
                    lock (this.thisLock)
                    {
                        if (this.file != null)
                        {
                            this.file.Dispose();
                            this.file = null;
                        }
                    }
                }
            }
            finally
            {
                base.Dispose(true);
            }
        }

        private static IRow FilterColumns(IRow row, IReadOnlyCollection<string> columnNames)
        {
            if (null == row)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameRow);
            }

            if (null == columnNames)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameColumnNames);
            }

            Dictionary<string, string> columns;
            if (row.Columns != null)
            {
                columns =
                    row
                    .Columns
                    .Where(
                        (KeyValuePair<string, string> columnItem) =>
                            columnNames
                            .Any(
                                (string columnNameItem) =>
                                    string.Equals(
                                        columnNameItem,
                                        columnItem.Key,
                                        StringComparison.Ordinal)))
                    .ToDictionary(
                        (KeyValuePair<string, string> item) =>
                            item.Key,
                        (KeyValuePair<string, string> item) =>
                            item.Value);
            }
            else
            {
                columns = null;
            }

            IRow result = new Row(row.Key, columns);
            return result;
        }

        private IReadOnlyCollection<string> IdentifyRequestedColumns(IRetrievalParameters parameters)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameParameters);
            }

            IEnumerable<string> requestedAttributes = this.attributeNames;
            if (parameters.RequestedAttributePaths != null && parameters.RequestedAttributePaths.Any())
            {
                requestedAttributes =
                    requestedAttributes
                    .Where(
                        (string candidateItem) =>
                            parameters
                            .RequestedAttributePaths
                            .Any(
                                (string requestedItem) =>
                                    string.Equals(candidateItem, requestedItem, StringComparison.Ordinal)))
                    .ToArray();
            }

            if (parameters.ExcludedAttributePaths != null)
            {
                requestedAttributes =
                    requestedAttributes
                    .Where(
                        (string candidateItem) =>
                            !parameters
                            .ExcludedAttributePaths
                            .Any(
                                (string excludedItem) =>
                                    string.Equals(candidateItem, excludedItem, StringComparison.Ordinal)))
                    .ToArray();
            }

            if
            (
                    FileProvider
                    .SchemaIdentifiersGroup
                    .Value
                    .Any(
                        (string item) =>
                            string.Equals(item, parameters.SchemaIdentifier, StringComparison.Ordinal))
                && requestedAttributes
                    .Any(
                        (string item) =>
                            string.Equals(AttributeNames.Members, item, StringComparison.Ordinal))
            )
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionRetrievingGroupMembersNotSupported);
            }

            IReadOnlyCollection<string> results = requestedAttributes.ToArray();
            return results;
        }

        private void OnServiceStartup(IAppBuilder applicationBuilder, HttpConfiguration configuration)
        {
        }

        public override async Task<Resource[]> Query(IQueryParameters parameters, string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameCorrelationIdentifier);
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            string informationAlternateFilterCount = parameters.AlternateFilters.Count.ToString(CultureInfo.InvariantCulture);
            ProvisioningAgentMonitor.Instance.Inform(informationAlternateFilterCount, true, correlationIdentifier);
            if (parameters.AlternateFilters.Count != 1)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        ProvisioningAgentResources.ExceptionFilterCountTemplate,
                        1,
                        parameters.AlternateFilters.Count);
                throw new NotSupportedException(exceptionMessage);
            }

            Resource[] results;
            IFilter queryFilter = parameters.AlternateFilters.Single();
            if (queryFilter.AdditionalFilter != null)
            {
                results = await this.QueryReference(parameters, correlationIdentifier);
                return results;
            }

            IReadOnlyCollection<string> requestedColumns = this.IdentifyRequestedColumns(parameters);

            if (string.IsNullOrWhiteSpace(queryFilter.AttributePath))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(queryFilter.ComparisonValue))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            Dictionary<string, string> columns =
                new Dictionary<string, string>()
                    {
                        {
                            AttributeNames.Schemas,
                            parameters.SchemaIdentifier
                        },
                        {
                            queryFilter.AttributePath,
                            queryFilter.ComparisonValue
                        }
                    };
            IReadOnlyCollection<IRow> rows = await this.file.Query(columns);
            if (null == rows)
            {
                return new Resource[0];
            }

            IList<Resource> resources = new List<Resource>(rows.Count);
            foreach (IRow row in rows)
            {
                string rowSchema = null;
                if
                (
                        !row.Columns.TryGetValue(AttributeNames.Schemas, out rowSchema)
                    ||  !string.Equals(rowSchema, parameters.SchemaIdentifier, StringComparison.Ordinal)
                )
                {
                    continue;
                }

                IRow reducedRow = FileProvider.FilterColumns(row, requestedColumns);

                ResourceFactory resourceFactory;
                switch (rowSchema)
                {
                    case SchemaIdentifiers.Core2EnterpriseUser:
                        resourceFactory = new UserFactory(reducedRow);
                        break;
                    case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                        resourceFactory = new GroupFactory(reducedRow);
                        break;
                    default:
                        throw new NotSupportedException(parameters.SchemaIdentifier);
                }

                Resource resource = resourceFactory.CreateResource();
                resources.Add(resource);
            }

            results = resources.ToArray();
            return results;
        }

        private async Task<Resource[]> QueryReference(IQueryParameters parameters, string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameCorrelationIdentifier);
            }

            if (null == parameters.RequestedAttributePaths || !parameters.RequestedAttributePaths.Any())
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
            }

            string selectedAttribute = parameters.RequestedAttributePaths.SingleOrDefault();
            if (string.IsNullOrWhiteSpace(selectedAttribute))
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
            }
            ProvisioningAgentMonitor.Instance.Inform(selectedAttribute, true, correlationIdentifier);

            if
            (
                !string.Equals(
                    selectedAttribute,
                    Microsoft.SystemForCrossDomainIdentityManagement.AttributeNames.Identifier,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            string informationAlternateFilterCount = parameters.AlternateFilters.Count.ToString(CultureInfo.InvariantCulture);
            ProvisioningAgentMonitor.Instance.Inform(informationAlternateFilterCount, true, correlationIdentifier);
            if (parameters.AlternateFilters.Count != 1)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        ProvisioningAgentResources.ExceptionFilterCountTemplate,
                        1,
                        parameters.AlternateFilters.Count);
                throw new NotSupportedException(exceptionMessage);
            }

            IReadOnlyCollection<string> requestedColumns = this.IdentifyRequestedColumns(parameters);

            IFilter filterPrimary = parameters.AlternateFilters.Single();
            if (null == filterPrimary.AdditionalFilter)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            IFilter filterAdditional = filterPrimary.AdditionalFilter;

            if (filterAdditional.AdditionalFilter != null)
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
            }

            IReadOnlyCollection<IFilter> filters =
                new IFilter[]
                    {
                        filterPrimary,
                        filterAdditional
                    };

            IFilter filterIdentifier =
                filters
                .SingleOrDefault(
                    (IFilter item) =>
                        string.Equals(
                            AttributeNames.Identifier,
                            item.AttributePath,
                            StringComparison.OrdinalIgnoreCase));
            if (null == filterIdentifier)
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
            }

            IRow row;
            IFilter filterReference =
                filters
                .SingleOrDefault(
                    (IFilter item) =>
                            string.Equals(
                                AttributeNames.Members,
                                item.AttributePath,
                                StringComparison.OrdinalIgnoreCase));
            if (filterReference != null)
            {
                Dictionary<string, string> columns =
                new Dictionary<string, string>()
                    {
                        {
                            AttributeNames.Schemas,
                            parameters.SchemaIdentifier
                        },
                        {
                            AttributeNames.Identifier,
                            filterIdentifier.ComparisonValue
                        },
                        {
                            filterReference.AttributePath,
                            filterReference.ComparisonValue
                        }
                    };

                IReadOnlyCollection<IRow> rows = await this.file.Query(columns);
                if (null == rows || !rows.Any())
                {
                    return new Resource[0];
                }

                row = await this.file.ReadRow(filterIdentifier.ComparisonValue);
            }
            else
            {
                filterReference =
                    filters
                    .SingleOrDefault(
                        (IFilter item) =>
                                string.Equals(
                                    AttributeNames.Manager,
                                    item.AttributePath,
                                    StringComparison.OrdinalIgnoreCase));
                if (null == filterReference)
                {
                    throw new NotSupportedException(ProvisioningAgentResources.ExceptionUnsupportedQuery);
                }

                row = await this.file.ReadRow(filterIdentifier.ComparisonValue);
                if
                (
                        null == row.Columns
                    || !row
                        .Columns
                        .Any(
                            (KeyValuePair<string, string> columnItem) =>
                                    string.Equals(columnItem.Key, filterReference.AttributePath, StringComparison.Ordinal)
                                && string.Equals(columnItem.Value, filterReference.ComparisonValue, StringComparison.Ordinal))
                )
                {
                    return new Resource[0];
                }
            }

            string rowSchema = null;
            if
            (
                    !row.Columns.TryGetValue(AttributeNames.Schemas, out rowSchema)
                || !string.Equals(rowSchema, parameters.SchemaIdentifier, StringComparison.Ordinal)
            )
            {
                return new Resource[0];
            }

            IRow reducedRow = FileProvider.FilterColumns(row, requestedColumns);

            ResourceFactory resourceFactory;
            switch (rowSchema)
            {
                case SchemaIdentifiers.Core2EnterpriseUser:
                    resourceFactory = new UserFactory(reducedRow);
                    break;
                case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                    resourceFactory = new GroupFactory(reducedRow);
                    break;
                default:
                    throw new NotSupportedException(parameters.SchemaIdentifier);
            }

            Resource resource = resourceFactory.CreateResource();
            Resource[] results =
                new Resource[]
                {
                    resource
                };
            return results;
        }

        public override async Task<Resource> Retrieve(IResourceRetrievalParameters parameters, string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameCorrelationIdentifier);
            }

            if (null == parameters.ResourceIdentifier)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            string informationStarting =
                string.Format(
                    CultureInfo.InvariantCulture,
                    FileProvisioningAgentResources.InformationRetrieving,
                    parameters.SchemaIdentifier,
                    parameters.ResourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            IReadOnlyCollection<string> columnNames = this.IdentifyRequestedColumns(parameters);
            
            IRow row = await this.file.ReadRow(parameters.ResourceIdentifier.Identifier);
            if (null == row || null == row.Columns)
            {
                return null;
            }

            string rowSchema = null;
            if
            (
                    !row.Columns.TryGetValue(AttributeNames.Schemas, out rowSchema)
                ||  !string.Equals(rowSchema, parameters.SchemaIdentifier, StringComparison.Ordinal)
            )
            {
                return null;
            }

            IRow reducedRow = FileProvider.FilterColumns(row, columnNames);

            ResourceFactory resourceFactory;
            switch (rowSchema)
            {
                case SchemaIdentifiers.Core2EnterpriseUser:
                    resourceFactory = new UserFactory(reducedRow);
                    break;
                case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                    resourceFactory = new GroupFactory(reducedRow);
                    break;
                default:
                    throw new NotSupportedException(parameters.SchemaIdentifier);
            }

            Resource result = resourceFactory.CreateResource();
            return result;
        }

        public override async Task Update(IPatch patch, string correlationIdentifier)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNamePatch);
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            string informationStarting =
                string.Format(
                    CultureInfo.InvariantCulture,
                    FileProvisioningAgentResources.InformationPatching,
                    patch.ResourceIdentifier.SchemaIdentifier,
                    patch.ResourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            PatchRequest2 patchRequest = patch.PatchRequest as PatchRequest2;
            if (null == patchRequest)
            {
                string unsupportedPatchTypeName = patch.GetType().FullName;
                throw new NotSupportedException(unsupportedPatchTypeName);
            }

            IRow row = await this.file.ReadRow(patch.ResourceIdentifier.Identifier);

            string rowSchema = null;
            if
            (
                    !row.Columns.TryGetValue(AttributeNames.Schemas, out rowSchema)
                ||  !string.Equals(rowSchema, patch.ResourceIdentifier.SchemaIdentifier, StringComparison.Ordinal)
            )
            {
                return;
            }

            IReadOnlyDictionary<string, string> columns;
            WindowsAzureActiveDirectoryGroup group = null;
            switch (rowSchema)
            {
                case SchemaIdentifiers.Core2EnterpriseUser:
                    ResourceFactory<Core2EnterpriseUser> userFactory = new UserFactory(row);
                    Core2EnterpriseUser user = userFactory.Create();
                    user.Apply(patchRequest);
                    ColumnsFactory<Core2EnterpriseUser> userColumnsFactory = new UserColumnsFactory(user);
                    columns = userColumnsFactory.CreateColumns();
                    break;

                case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                    ResourceFactory<WindowsAzureActiveDirectoryGroup> groupFactory = new GroupFactory(row);
                    group = groupFactory.Create();
                    group.Apply(patchRequest);
                    ColumnsFactory<WindowsAzureActiveDirectoryGroup> groupColumnsFactory = new GroupColumnsFactory(group);
                    columns = groupColumnsFactory.CreateColumns();
                    break;
                default:
                    throw new NotSupportedException(patch.ResourceIdentifier.SchemaIdentifier);
            }

            IRow rowReplacement = new Row(row.Key, columns);
            await this.file.ReplaceRow(rowReplacement);

            if (group != null)
            {
                await this.UpdateMembers(group, patch);
            }
        }

        private async Task UpdateMembers(Resource resource, IPatch patch)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNameResource);
            }

            if (null == patch)
            {
                throw new ArgumentNullException(FileProvider.ArgumentNamePatch);
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidPatch);
            }

            PatchRequest2 patchRequest = patch.PatchRequest as PatchRequest2;
            if (null == patchRequest)
            {
                string unsupportedPatchTypeName = patch.GetType().FullName;
                throw new NotSupportedException(unsupportedPatchTypeName);
            }

            if
            (
                !FileProvider
                .SchemaIdentifiersGroup
                .Value
                .Any(
                    (string item) =>
                        string.Equals(item, patch.ResourceIdentifier.SchemaIdentifier, StringComparison.Ordinal))
            )
            {
                return;
            }

            IReadOnlyCollection<PatchOperation> memberOperations =
                patchRequest
                .Operations
                .Where(
                    (PatchOperation item) =>
                            item.Path != null
                        && string.Equals(item.Path.AttributePath, AttributeNames.Members, StringComparison.Ordinal))
                .ToArray();
            if (!memberOperations.Any())
            {
                return;
            }

            foreach (PatchOperation memberOperation in memberOperations)
            {
                if (null == memberOperation.Value)
                {
                    continue;
                }
                
                foreach (OperationValue value in memberOperation.Value)
                {
                    if (string.IsNullOrWhiteSpace(value.Value))
                    {
                        continue;
                    }

                    Dictionary<string, string> columnsQuery = 
                        new Dictionary<string,string>()
                            {
                                {
                                    AttributeNames.Schemas,
                                    patch.ResourceIdentifier.SchemaIdentifier
                                },
                                {
                                    AttributeNames.Identifier,
                                    patch.ResourceIdentifier.Identifier
                                },
                                {
                                    AttributeNames.Members,
                                    value.Value
                                }
                            };
                    IRow[] rows = await this.file.Query(columnsQuery);

                    switch (memberOperation.Name)
                    {
                        case OperationName.Add:
                            if (rows.Any())
                            {
                                break;
                            }

                            Member member = 
                                new Member()
                                    {
                                        Value = value.Value
                                    };
                            MemberColumnsFactory memberColumnsFactory = new MemberColumnsFactory(resource, member);
                            IReadOnlyDictionary<string, string> columnsMember = memberColumnsFactory.CreateColumns();
                            await this.file.InsertRow(columnsMember);
                            break;

                        case OperationName.Remove:
                            foreach (IRow row in rows)
                            {
                                await this.file.RemoveRow(row.Key);
                            }

                            break;
                    }
                }
            }
        }
    }
}
