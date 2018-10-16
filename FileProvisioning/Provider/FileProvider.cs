//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Graph.Provisioning;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Owin;
    using Samples.Properties;

    public class FileProvider: FileProviderBase
    {
        private const long NotificationIdentifierCreationStarting = 1000;
        private const long NotificationIdentifierDeletionStarting = 1001;
        private const long NotificationIdentifierQueryFilterCount = 1002;
        private const long NotificationIdentifierReferenceQueryAttribute = 1003;
        private const long NotificationIdentifierReferenceQueryFilterCount = 1004;
        private const long NotificationIdentifierRetrievalStarting = 1005;
        private const long NotificationIdentifierUpdateStarting = 1005;

        private static readonly Lazy<IReadOnlyCollection<IExtension>> ExtensionsSingleton =
            new Lazy<IReadOnlyCollection<IExtension>>(
                () =>
                    new IExtension[]
                    {
                        DynamicUsersExtension.Instance
                    });

        private static readonly Lazy<IReadOnlyCollection<string>> SchemaIdentifiersGroup =
            new Lazy<IReadOnlyCollection<string>>(
                () =>
                    new string[]
                    {
                        SchemaIdentifiers.Core2Group,
                        SchemaIdentifiers.WindowsAzureActiveDirectoryGroup
                    });

        private readonly object thisLock = new object();

        private ITabularFileAdapter file;

        public FileProvider(
            TabularFileAdapterFactory fileAdapterFactory,
            IMonitor monitor, 
            IReadOnlyCollection<string> attributeNames)
        {
            if (null == fileAdapterFactory)
            {
                throw new ArgumentNullException(nameof(fileAdapterFactory));
            }

            if (null == monitor)
            {
                throw new ArgumentNullException(nameof(monitor));
            }

            this.Monitor = monitor;

            Type typeAttributeNames = typeof(AttributeNames);
            IReadOnlyCollection<string> buffer =
                typeAttributeNames.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(
                    (FieldInfo item) =>
                        (string)item.GetValue(null))
                .ToArray();
            this.ColumnNames =
                buffer
                .Union(attributeNames)
                .Distinct()
                .ToArray();

            this.file = fileAdapterFactory.CreateFileAdapter(this.ColumnNames);
        }

        public FileProvider(
            TabularFileAdapterFactory fileAdapterFactory, 
            IMonitor monitor)
            :this(fileAdapterFactory, monitor, new string[0])
        {
        }

        private IReadOnlyCollection<string> ColumnNames
        {
            get;
            set;
        }

        public override IReadOnlyCollection<IExtension> Extensions
        {
            get
            {
                return FileProvider.ExtensionsSingleton.Value;
            }
        }

        public override string FilePath
        {
            get
            {
                return this.file.FilePath;
            }
        }

        private IMonitor Monitor
        {
            get;
            set;
        }

        public override Action<IAppBuilder, HttpConfiguration> StartupBehavior
        {
            get 
            {
                return this.OnServiceStartup;
            }
        }

        private async Task AddMembersOf(Resource resource)
        {
            WindowsAzureActiveDirectory2Group group = resource as WindowsAzureActiveDirectory2Group;
            if (group != null && group.Members != null && group.Members.Any())
            {
                foreach (Member member in group.Members)
                {
                    MemberColumnsFactory memberColumnsFactory = new MemberColumnsFactory(resource, member);
                    IReadOnlyDictionary<string, string> memberColumns = memberColumnsFactory.CreateColumns();
                    await this.file.InsertRow(memberColumns);
                }
            }
        }

        private static IReadOnlyDictionary<string, string> Apply(
            PatchRequest2Base<PatchOperation2> patch, 
            string schemaIdentifier, IRow row)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (string.IsNullOrWhiteSpace(schemaIdentifier))
            {
                throw new ArgumentNullException(nameof(schemaIdentifier));
            }

            IReadOnlyDictionary<string, string> result;
            switch (schemaIdentifier)
            {
                case SchemaIdentifiers.Core2EnterpriseUser:
                    result = FileProvider.PatchUser(patch, row);
                    return result;

                case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                    result = FileProvider.PatchGroup(patch, row);
                    return result;

                default:
                    throw new NotSupportedException(schemaIdentifier);
            }
            
        }

        public override async Task<Resource> CreateAsync(Resource resource, string correlationIdentifier)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (string.IsNullOrWhiteSpace(resource.ExternalIdentifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidResource);
            }

            IInformationNotification notification = 
                VerboseInformationNotificationFactory.Instance.FormatNotification(
                    FileProviderResources.InformationCreating, 
                    correlationIdentifier, 
                    FileProvider.NotificationIdentifierCreationStarting,
                    resource.ExternalIdentifier);
            this.Monitor.Inform(notification);

            ColumnsFactory columnsFactory = FileProvider.SelectColumnsFactoryFor(resource);
            IReadOnlyDictionary<string, string> columns = columnsFactory.CreateColumns();
            IRow row = await this.file.InsertRow(columns);

            ResourceFactory resourceFactory = FileProvider.SelectResourceFactoryFor(resource, row);
            Resource result = resourceFactory.CreateResource();

            await this.AddMembersOf(result);

            return result;
        }

        public override async Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentNullException(nameof(resourceIdentifier));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.Identifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidResource);
            }

            IInformationNotification notification =
                VerboseInformationNotificationFactory.Instance.FormatNotification(
                    FileProviderResources.InformationDeleting,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierDeletionStarting,
                    resourceIdentifier.SchemaIdentifier,
                    resourceIdentifier.Identifier);
            this.Monitor.Inform(notification);

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
                throw new ArgumentNullException(nameof(row));
            }

            if (null == columnNames)
            {
                throw new ArgumentNullException(nameof(columnNames));
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
                throw new ArgumentNullException(nameof(parameters));
            }

            IEnumerable<string> requestedAttributes = this.ColumnNames;
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
                throw new NotSupportedException(FileProviderResources.ExceptionRetrievingGroupMembersNotSupported);
            }

            IReadOnlyCollection<string> results = requestedAttributes.ToArray();
            return results;
        }

        private void OnServiceStartup(IAppBuilder applicationBuilder, HttpConfiguration configuration)
        {
        }

        private static IReadOnlyDictionary<string, string> PatchGroup(PatchRequest2Base<PatchOperation2> patch, IRow row)
        {
            ResourceFactory<WindowsAzureActiveDirectoryGroup> groupFactory = new GroupFactory(row);
            WindowsAzureActiveDirectoryGroup group = groupFactory.Create();
            group.Apply(patch);
            ColumnsFactory<WindowsAzureActiveDirectoryGroup> groupColumnsFactory = new GroupColumnsFactory(group);
            IReadOnlyDictionary<string, string> result = groupColumnsFactory.CreateColumns();
            return result;
        }

        private static IReadOnlyDictionary<string, string> PatchUser(PatchRequest2Base<PatchOperation2> patch, IRow row)
        {
            ResourceFactory<Core2EnterpriseUser> userFactory = new UserFactory(row);
            Core2EnterpriseUser user = userFactory.Create();
            user.Apply(patch);
            ColumnsFactory<Core2EnterpriseUser> userColumnsFactory = new UserColumnsFactory(user);
            IReadOnlyDictionary<string, string> result = userColumnsFactory.CreateColumns();
            return result;
        }

        public override async Task<Resource[]> QueryAsync(IQueryParameters parameters, string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            string informationAlternateFilterCount = parameters.AlternateFilters.Count.ToString(CultureInfo.InvariantCulture);
            IInformationNotification notification =
                VerboseInformationNotificationFactory.Instance.CreateNotification(
                    informationAlternateFilterCount,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierQueryFilterCount);
            this.Monitor.Inform(notification);
            if (parameters.AlternateFilters.Count != 1)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FileProviderResources.ExceptionFilterCountTemplate,
                        1,
                        parameters.AlternateFilters.Count);
                throw new NotSupportedException(exceptionMessage);
            }

            Resource[] results;
            IFilter queryFilter = parameters.AlternateFilters.Single();
            if (queryFilter.AdditionalFilter != null)
            {
                results = await this.QueryReferenceAsync(parameters, correlationIdentifier);
                return results;
            }

            IReadOnlyCollection<string> requestedColumns = this.IdentifyRequestedColumns(parameters);

            if (string.IsNullOrWhiteSpace(queryFilter.AttributePath))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(queryFilter.ComparisonValue))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
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
                ResourceFactory resourceFactory = FileProvider.SelectResourceFactoryFor(rowSchema, reducedRow);
                Resource resource = resourceFactory.CreateResource();
                resources.Add(resource);
            }

            results = resources.ToArray();
            return results;
        }

        private async Task<Resource[]> QueryReferenceAsync(IQueryParameters parameters, string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (null == parameters.RequestedAttributePaths || !parameters.RequestedAttributePaths.Any())
            {
                throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
            }

            string selectedAttribute = parameters.RequestedAttributePaths.SingleOrDefault();
            if (string.IsNullOrWhiteSpace(selectedAttribute))
            {
                throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
            }
            IInformationNotification notificationReferenceQueryAttribute =
                VerboseInformationNotificationFactory.Instance.CreateNotification(
                    selectedAttribute,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierReferenceQueryAttribute);
            this.Monitor.Inform(notificationReferenceQueryAttribute);

            if
            (
                !string.Equals(
                    selectedAttribute,
                    AttributeNames.Identifier,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            string informationAlternateFilterCount = parameters.AlternateFilters.Count.ToString(CultureInfo.InvariantCulture);
            IInformationNotification notificationReferenceQueryFilterCount =
                VerboseInformationNotificationFactory.Instance.CreateNotification(
                    informationAlternateFilterCount,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierReferenceQueryFilterCount);
            this.Monitor.Inform(notificationReferenceQueryFilterCount);
            if (parameters.AlternateFilters.Count != 1)
            {
                string exceptionMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FileProviderResources.ExceptionFilterCountTemplate,
                        1,
                        parameters.AlternateFilters.Count);
                throw new NotSupportedException(exceptionMessage);
            }

            IReadOnlyCollection<string> requestedColumns = this.IdentifyRequestedColumns(parameters);

            IFilter filterPrimary = parameters.AlternateFilters.Single();
            if (null == filterPrimary.AdditionalFilter)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            IFilter filterAdditional = filterPrimary.AdditionalFilter;

            if (filterAdditional.AdditionalFilter != null)
            {
                throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
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
                throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
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
                    throw new NotSupportedException(FileProviderResources.ExceptionUnsupportedQuery);
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
                                &&  string.Equals(columnItem.Value, filterReference.ComparisonValue, StringComparison.Ordinal))
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
            ResourceFactory resourceFactory = FileProvider.SelectResourceFactoryFor(rowSchema, reducedRow);
            Resource resource = resourceFactory.CreateResource();
            Resource[] results =
                new Resource[]
                {
                    resource
                };
            return results;
        }

        public override async Task<Resource> RetrieveAsync(
            IResourceRetrievalParameters parameters, 
            string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (null == parameters.ResourceIdentifier)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidParameters);
            }

            IInformationNotification notification =
                VerboseInformationNotificationFactory.Instance.FormatNotification(
                    FileProviderResources.InformationRetrieving,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierRetrievalStarting,
                    parameters.SchemaIdentifier,
                    parameters.ResourceIdentifier.Identifier);
            this.Monitor.Inform(notification);

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
            ResourceFactory resourceFactory = FileProvider.SelectResourceFactoryFor(rowSchema, reducedRow);
            Resource result = resourceFactory.CreateResource();
            return result;
        }

        private static ColumnsFactory SelectColumnsFactoryFor(Resource resource)
        {
            WindowsAzureActiveDirectoryGroup group = resource as WindowsAzureActiveDirectoryGroup;
            if (group != null)
            {
                ColumnsFactory result = new GroupColumnsFactory(group);
                return result;
            }
            
            Core2EnterpriseUser user = resource as Core2EnterpriseUser;
            if (user != null)
            {
                ColumnsFactory result = new UserColumnsFactory(user);
                return result;
            }

            string unsupportedSchema =
                        string.Join(
                            Environment.NewLine,
                            resource.Schemas);
            throw new NotSupportedException(unsupportedSchema);
        }

        private static ResourceFactory SelectResourceFactoryFor(Resource resource, IRow row)
        {
            WindowsAzureActiveDirectoryGroup group = resource as WindowsAzureActiveDirectoryGroup;
            if (group != null)
            {
                ResourceFactory result = new GroupFactory(row);
                return result;
            }

            Core2EnterpriseUser user = resource as Core2EnterpriseUser;
            if (user != null)
            {
                ResourceFactory result = new UserFactory(row);
                return result;
            }

            string unsupportedSchema =
                        string.Join(
                            Environment.NewLine,
                            resource.Schemas);
            throw new NotSupportedException(unsupportedSchema);
        }

        private static ResourceFactory SelectResourceFactoryFor(string schemaIdentifier, IRow row)
        {
            if (string.IsNullOrWhiteSpace(schemaIdentifier))
            {
                throw new ArgumentNullException(nameof(schemaIdentifier));
            }

            ResourceFactory resourceFactory;
            switch (schemaIdentifier)
            {
                case SchemaIdentifiers.Core2EnterpriseUser:
                    resourceFactory = new UserFactory(row);
                    break;
                case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                    resourceFactory = new GroupFactory(row);
                    break;
                default:
                    throw new NotSupportedException(schemaIdentifier);
            }
            return resourceFactory;
        }

        public override async Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            IInformationNotification notification =
                VerboseInformationNotificationFactory.Instance.FormatNotification(
                    FileProviderResources.InformationPatching,
                    correlationIdentifier,
                    FileProvider.NotificationIdentifierUpdateStarting,
                    patch.ResourceIdentifier.SchemaIdentifier,
                    patch.ResourceIdentifier.Identifier);
            this.Monitor.Inform(notification);

            PatchRequest2Base<PatchOperation2> patchRequest = 
                patch.PatchRequest as PatchRequest2Base<PatchOperation2>;

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

            IReadOnlyDictionary<string, string> columns = FileProvider.Apply(patchRequest, rowSchema, row);
            IRow rowReplacement = new Row(row.Key, columns);
            await this.file.ReplaceRow(rowReplacement);

            if (string.Equals(SchemaIdentifiers.WindowsAzureActiveDirectoryGroup, rowSchema, StringComparison.OrdinalIgnoreCase))
            {
                WindowsAzureActiveDirectoryGroup group = new GroupFactory(row).Create();
                await this.UpdateMembersAsync(group, patch);
            }
        }

        private async Task UpdateMembersAsync(Resource resource, IPatch patch)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (null == patch)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(FileProviderResources.ExceptionInvalidPatch);
            }

            PatchRequest2Legacy patchRequest = patch.PatchRequest as PatchRequest2Legacy;
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

            IReadOnlyCollection<PatchOperation2> memberOperations =
                patchRequest
                .Operations
                .Where(
                    (PatchOperation2 item) =>
                            item.Path != null
                        && string.Equals(item.Path.AttributePath, AttributeNames.Members, StringComparison.Ordinal))
                .ToArray();
            if (!memberOperations.Any())
            {
                return;
            }

            foreach (PatchOperation2 memberOperation in memberOperations)
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
                        new Dictionary<string, string>()
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
