//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Owin;

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Amazon;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;
    using Amazon.Runtime;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.ActiveDirectory;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Samples.Properties;
    
    public class AmazonWebServicesProvider: AmazonWebServicesProviderBase
    {
        private const string ArgumentNameAnchoringBehavior = "anchoringBehavior";
        private const string ArgumentNameApplicationBuilder = "applicationBuilder";
        private const string ArgumentNameAuthenticationOptions = "authenticationOptions";
        private const string ArgumentNameConfiguration = "configuration";
        private const string ArgumentNameCorrelationIdentifier = "correlationIdentifier";
        private const string ArgumentNameCredentialsProfileName = "credentialsProfileName";
        private const string ArgumentNameGroupName = "groupName";
        private const string ArgumentNameMember = "member";
        private const string ArgumentNameMemberIdentifier = "memberIdentifier";
        private const string ArgumentNameMembers = "members";
        private const string ArgumentNameMembershipOperation = "membershipOperation";
        private const string ArgumentNameParameters = "parameters";
        private const string ArgumentNamePatch = "patch";
        private const string ArgumentNameProxy = "proxy";
        private const string ArgumentNameResource = "resource";
        private const string ArgumentNameResourceIdentifier = "resourceIdentifier";
        
        private const int SizeListPage = 100;

        private IAmazonWebServicesIdentityAnchoringBehavior anchoringBehaviorValue;
        private WindowsAzureActiveDirectoryBearerAuthenticationOptions windowsAzureActiveDirectoryBearerAuthenticationOptions;
        private AWSCredentials credentials;

        public AmazonWebServicesProvider(string credentialsProfileName)
        {
            if (string.IsNullOrWhiteSpace(credentialsProfileName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCredentialsProfileName);
            }

            this.anchoringBehaviorValue = new AnchoringByIdentifierBehavior();
            AuthenticationOptions authenticationOptions = new NoAuthentication();
            this.Initialize(credentialsProfileName, this.anchoringBehaviorValue, authenticationOptions);
        }

        public AmazonWebServicesProvider(
            string credentialsProfileName, 
            WindowsAzureActiveDirectoryBearerAuthenticationOptions authenticationOptions)
        {
            if (string.IsNullOrWhiteSpace(credentialsProfileName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCredentialsProfileName);
            }

            if (null == authenticationOptions)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAuthenticationOptions);
            }

            this.anchoringBehaviorValue = new AnchoringByIdentifierBehavior();
            this.Initialize(credentialsProfileName, this.anchoringBehaviorValue, authenticationOptions);
        }

        public AmazonWebServicesProvider(
            string credentialsProfileName,
            IAmazonWebServicesIdentityAnchoringBehavior anchoringBehavior,
            WindowsAzureActiveDirectoryBearerAuthenticationOptions authenticationOptions)
        {
            if (string.IsNullOrWhiteSpace(credentialsProfileName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCredentialsProfileName);
            }

            if (null == anchoringBehavior)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAnchoringBehavior);
            }

            if (null == authenticationOptions)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAuthenticationOptions);
            }

            this.Initialize(credentialsProfileName, anchoringBehavior, authenticationOptions);
        }

        public AmazonWebServicesProvider(
            string credentialsProfileName,
            IAmazonWebServicesIdentityAnchoringBehavior anchoringBehavior)
        {
            if (string.IsNullOrWhiteSpace(credentialsProfileName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCredentialsProfileName);
            }

            if (null == anchoringBehavior)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAnchoringBehavior);
            }

            AuthenticationOptions authenticationOptions = new NoAuthentication();
            this.Initialize(credentialsProfileName, anchoringBehavior, authenticationOptions);
        }

        public override IAmazonWebServicesIdentityAnchoringBehavior AnchoringBehavior
        {
            get
            {
                return this.anchoringBehaviorValue;
            }
        }

        public override Action<IAppBuilder, HttpConfiguration> StartupBehavior
        {
            get 
            {
                return this.OnServiceStartup;
            }
        }

        private async Task AddMember(
            string groupName, 
            string memberIdentifier, 
            IAmazonIdentityManagementService proxy,
            string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameGroupName);
            }

            if (string.IsNullOrWhiteSpace(memberIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameMemberIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            Amazon.IdentityManagement.Model.User memberUser = await this.RetrieveUser(memberIdentifier, proxy);
            if (null == memberUser)
            {
                string warning =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        AmazonProvisioningAgentResources.WarningEntryNotFoundTemplate,
                        typeof(Amazon.IdentityManagement.Model.User).Name,
                        memberIdentifier);
                ProvisioningAgentMonitor.Instance.Warn(warning, correlationIdentifier);
                return;
            }

            AddUserToGroupRequest request = new AddUserToGroupRequest(groupName, memberUser.UserName);
            await proxy.AddUserToGroupAsync(request);
        }

        private async Task AddMember(
            string groupName, 
            Member member, 
            IAmazonIdentityManagementService proxy,
            string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameGroupName);
            }

            if (null == member)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameMember);
            }

            if (string.IsNullOrWhiteSpace(member.Value))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidMember);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            await this.AddMember(groupName, member.Value, proxy, correlationIdentifier);
        }

        private async Task AddMembers(
            string groupName, 
            IEnumerable<Member> members, 
            IAmazonIdentityManagementService proxy,
            string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameGroupName);
            }

            if (null == members)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameMembers);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            foreach (Member member in members)
            {
                await this.AddMember(groupName, member, proxy, correlationIdentifier);
            }
        }

        public override async Task<Resource> Create(
            Resource resource, 
            string correlationIdentifier)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameResource);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resource.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResource);
            }

            string informationStarting =
                string.Format(
                    CultureInfo.InvariantCulture,
                    AmazonProvisioningAgentResources.InformationCreating,
                    resource.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                WindowsAzureActiveDirectoryGroup group = resource as WindowsAzureActiveDirectoryGroup;
                if (group != null)
                {
                    CreateGroupRequest request = new CreateGroupRequest(group.ExternalIdentifier);
                    CreateGroupResult response = await proxy.CreateGroupAsync(request);
                    group.Identifier = this.AnchoringBehavior.Identify(response.Group);

                    if (group.Members != null && group.Members.Any())
                    {
                        await this.AddMembers(group.ExternalIdentifier, group.Members, proxy, correlationIdentifier);
                    }

                    return group;
                }

                UserBase user = resource as UserBase;
                if (user != null)
                {
                    CreateUserRequest request = new CreateUserRequest(user.ExternalIdentifier);
                    CreateUserResult response = await proxy.CreateUserAsync(request);
                    user.Identifier = this.AnchoringBehavior.Identify(response.User);
                    return user;
                }

                string unsupportedSchema =
                            string.Join(
                                Environment.NewLine,
                                resource.Schemas);
                throw new NotSupportedException(unsupportedSchema);
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        public override async Task Delete(
            IResourceIdentifier resourceIdentifier, 
            string correlationIdentifier)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResource);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResource);
            }

            string informationStarting =
                 string.Format(
                     CultureInfo.InvariantCulture,
                     AmazonProvisioningAgentResources.InformationDeleting,
                     resourceIdentifier.SchemaIdentifier,
                     resourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                string warning;
                switch (resourceIdentifier.SchemaIdentifier)
                {
                    case SchemaIdentifiers.Core2EnterpriseUser:
                        Amazon.IdentityManagement.Model.User user = 
                            await this.RetrieveUser(resourceIdentifier.Identifier, proxy);
                        if (null == user || string.IsNullOrWhiteSpace(user.UserName))
                        {
                            warning =
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    AmazonProvisioningAgentResources.WarningEntryNotFoundTemplate,
                                    typeof(Amazon.IdentityManagement.Model.User).Name,
                                    resourceIdentifier.Identifier);
                            ProvisioningAgentMonitor.Instance.Warn(warning, correlationIdentifier);
                            return;
                        }
                        DeleteUserRequest deleteRequestUser = new DeleteUserRequest(user.UserName);
                        await proxy.DeleteUserAsync(deleteRequestUser);
                        return;

                    case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                        Group group = 
                            await this.RetrieveGroup(resourceIdentifier.Identifier, proxy);
                        if (null == group || string.IsNullOrWhiteSpace(group.GroupName))
                        {
                            warning =
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    AmazonProvisioningAgentResources.WarningEntryNotFoundTemplate,
                                    typeof(Group).Name,
                                    resourceIdentifier.Identifier);
                            ProvisioningAgentMonitor.Instance.Warn(warning, correlationIdentifier);
                            return;
                        }
                        DeleteGroupRequest deleteRequestGroup = new DeleteGroupRequest(group.GroupName);
                        await proxy.DeleteGroupAsync(deleteRequestGroup);
                        return;

                    default:
                        throw new NotSupportedException(resourceIdentifier.SchemaIdentifier);
                }
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        private void Initialize(
            string credentialsProfileName,
            IAmazonWebServicesIdentityAnchoringBehavior anchoringBehavior,
            AuthenticationOptions authenticationOptions)
        {
            if (string.IsNullOrWhiteSpace(credentialsProfileName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCredentialsProfileName);
            }

            if (null == anchoringBehavior)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAnchoringBehavior);
            }

            if (null == authenticationOptions)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameAuthenticationOptions);
            }

            this.credentials = new StoredProfileAWSCredentials(credentialsProfileName);

            this.anchoringBehaviorValue = anchoringBehavior;
            
            this.windowsAzureActiveDirectoryBearerAuthenticationOptions = 
                authenticationOptions as WindowsAzureActiveDirectoryBearerAuthenticationOptions;
            if (this.windowsAzureActiveDirectoryBearerAuthenticationOptions != null)
            {
                this.windowsAzureActiveDirectoryBearerAuthenticationOptions.TokenHandler = new TokenHandler();
            }
        }

        private void OnServiceStartup(
            IAppBuilder applicationBuilder, 
            HttpConfiguration configuration)
        {
            if (null == applicationBuilder)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameApplicationBuilder);
            }

            if (null == configuration)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameConfiguration);
            }

            if (null == this.windowsAzureActiveDirectoryBearerAuthenticationOptions)
            {
                return;
            }

            System.Web.Http.Filters.IFilter authorizationFilter = new AuthorizeAttribute();
            configuration.Filters.Add(authorizationFilter);
            
            applicationBuilder
                .UseWindowsAzureActiveDirectoryBearerAuthentication(
                    this.windowsAzureActiveDirectoryBearerAuthenticationOptions);
        }

        public override async Task<Resource[]> Query(
            IQueryParameters parameters, 
            string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
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

            AmazonWebServicesProvider.Validate(parameters);

            if (string.IsNullOrWhiteSpace(queryFilter.AttributePath))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(queryFilter.ComparisonValue))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidParameters);
            }

            if (!string.Equals(queryFilter.AttributePath, AttributeNames.ExternalIdentifier, StringComparison.Ordinal))
            {
                throw new NotSupportedException(queryFilter.AttributePath);
            }

            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);            

                switch (parameters.SchemaIdentifier)
                {
                    case SchemaIdentifiers.Core2EnterpriseUser:
                        GetUserRequest getRequestUser =
                            new GetUserRequest()
                                {
                                    UserName = queryFilter.ComparisonValue
                                };
                        GetUserResult responseUser = await proxy.GetUserAsync(getRequestUser);
                        if (null == responseUser.User)
                        {
                            return new Resource[0];
                        }

                        Core2EnterpriseUser resourceUser =
                            new Core2EnterpriseUser()
                            {
                                Identifier = responseUser.User.UserId,
                                ExternalIdentifier = responseUser.User.UserName
                            };
                        Resource[] resourceUsers =
                            new Resource[]
                            {
                                resourceUser
                            };
                        return resourceUsers;

                    case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                        GetGroupRequest getRequestGroup =
                            new GetGroupRequest()
                                {
                                    GroupName = queryFilter.ComparisonValue
                                };
                        GetGroupResult responseGroup = await proxy.GetGroupAsync(getRequestGroup);
                        if (null == responseGroup.Group)
                        {
                            return new Resource[0];
                        }

                        WindowsAzureActiveDirectoryGroup resourceGroup =
                            new WindowsAzureActiveDirectoryGroup()
                            {
                                Identifier = responseGroup.Group.GroupId,
                                ExternalIdentifier = responseGroup.Group.GroupName
                            };
                        Resource[] resourceGroups =
                            new Resource[]
                            {
                                resourceGroup
                            };
                        return resourceGroups;

                    default:
                        throw new NotSupportedException(parameters.SchemaIdentifier);
                }
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        private async Task<Resource[]> QueryReference(
            IQueryParameters parameters, 
            string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
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

            AmazonWebServicesProvider.Validate(parameters);

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

            IFilter filterReference =
                filters
                .SingleOrDefault(
                    (IFilter item) =>
                            string.Equals(
                                AttributeNames.Members,
                                item.AttributePath,
                                StringComparison.OrdinalIgnoreCase));
            if (null == filterReference)
            {
                return new Resource[0];
            }
            
            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                Amazon.IdentityManagement.Model.User member = await this.RetrieveUser(filterReference.ComparisonValue, proxy);

                if (member != null)
                {
                    return new Resource[0];
                }

                ListGroupsForUserRequest request =
                    new ListGroupsForUserRequest()
                        {
                            MaxItems = AmazonWebServicesProvider.SizeListPage,
                            UserName = member.UserName
                        };
                while (true)
                {
                    ListGroupsForUserResponse response = await proxy.ListGroupsForUserAsync(request);
                    if (null == response.Groups || !response.Groups.Any())
                    {
                        return null;
                    }

                    Group group =
                        response
                        .Groups
                        .SingleOrDefault(
                            (Group item) =>
                                string.Equals(item.GroupName, filterReference.ComparisonValue, StringComparison.OrdinalIgnoreCase));
                    if (group != null)
                    {
                        WindowsAzureActiveDirectoryGroup groupResource =
                            new WindowsAzureActiveDirectoryGroup()
                                {
                                    Identifier = group.GroupId,
                                    ExternalIdentifier = group.GroupName
                                };
                        Resource[] results =
                            new Resource[]
                                {
                                    groupResource
                                };
                        return results;
                    }

                    if (string.IsNullOrWhiteSpace(response.Marker))
                    {
                        return null;
                    }

                    if (string.Equals(request.Marker, response.Marker, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    request.Marker = response.Marker;
                }
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        private async Task RemoveMember(
            string groupName,
            string memberIdentifier,
            IAmazonIdentityManagementService proxy,
            string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameGroupName);
            }

            if (string.IsNullOrWhiteSpace(memberIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameMemberIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            Amazon.IdentityManagement.Model.User memberUser = await this.RetrieveUser(memberIdentifier, proxy);
            if (null == memberUser)
            {
                string warning =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        AmazonProvisioningAgentResources.WarningEntryNotFoundTemplate,
                        typeof(Amazon.IdentityManagement.Model.User).Name,
                        memberIdentifier);
                ProvisioningAgentMonitor.Instance.Warn(warning, correlationIdentifier);
                return;
            }

            RemoveUserFromGroupRequest request = new RemoveUserFromGroupRequest(groupName, memberUser.UserName);
            await proxy.RemoveUserFromGroupAsync(request);
        }

        public override async Task<Resource> Retrieve(
            IResourceRetrievalParameters parameters, 
            string correlationIdentifier)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameParameters);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
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
                    AmazonProvisioningAgentResources.InformationRetrieving,
                    parameters.SchemaIdentifier,
                    parameters.ResourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            AmazonWebServicesProvider.Validate(parameters);

             IAmazonIdentityManagementService proxy = null;
             try
             {
                 proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                 switch (parameters.SchemaIdentifier)
                 {
                     case SchemaIdentifiers.Core2EnterpriseUser:
                         Amazon.IdentityManagement.Model.User user = 
                             await this.RetrieveUser(parameters.ResourceIdentifier.Identifier, proxy);
                         Core2EnterpriseUser resourceUser =
                             new Core2EnterpriseUser()
                                 {
                                     Identifier = user.UserId,
                                     ExternalIdentifier = user.UserName
                                 };
                         return resourceUser;

                     case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                         Group group = 
                             await this.RetrieveGroup(parameters.ResourceIdentifier.Identifier, proxy);
                         WindowsAzureActiveDirectoryGroup resourceGroup =
                             new WindowsAzureActiveDirectoryGroup()
                                 {
                                     Identifier = group.GroupId,
                                     ExternalIdentifier = group.GroupName
                                 };
                         return resourceGroup;

                     default:
                         throw new NotSupportedException(parameters.SchemaIdentifier);
                 }
             }
             finally
             {
                 if (proxy != null)
                 {
                     proxy.Dispose();
                     proxy = null;
                 }
             }
        }

        private async Task<Group> RetrieveGroup(
            string resourceIdentifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (string.IsNullOrWhiteSpace(resourceIdentifier))
            {
                throw new ArgumentException(AmazonWebServicesProvider.ArgumentNameResourceIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            Group result = await this.AnchoringBehavior.RetrieveGroup(resourceIdentifier, proxy);
            return result;
        }

        private async Task<Amazon.IdentityManagement.Model.User> RetrieveUser(
            string resourceIdentifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentException(AmazonWebServicesProvider.ArgumentNameResourceIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameProxy);
            }

            Amazon.IdentityManagement.Model.User result = await this.AnchoringBehavior.RetrieveUser(resourceIdentifier, proxy);
            return result;
        }

        public override async Task Update(
            IPatch patch,
            string correlationIdentifier)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNamePatch);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
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
                    AmazonProvisioningAgentResources.InformationPatching,
                    patch.ResourceIdentifier.SchemaIdentifier,
                    patch.ResourceIdentifier.Identifier);
            ProvisioningAgentMonitor.Instance.Inform(informationStarting, true, correlationIdentifier);

            PatchRequest2 patchRequest = patch.PatchRequest as PatchRequest2;
            if (null == patchRequest)
            {
                string unsupportedPatchTypeName = patch.GetType().FullName;
                throw new NotSupportedException(unsupportedPatchTypeName);
            }

            if (null == patchRequest.Operations)
            {
                return;
            }

            PatchOperation operation;
            
            operation =
                patchRequest
                .Operations
                .SingleOrDefault(
                    (PatchOperation item) =>
                            OperationName.Replace == item.Name
                        && item.Path != null
                        && string.Equals(item.Path.AttributePath, AttributeNames.ExternalIdentifier, StringComparison.Ordinal));
            if (operation != null)
            {
                string externalIdentifierValue = operation.Value.Single().Value;
                await this.UpdateExternalIdentifier(patch.ResourceIdentifier, correlationIdentifier, externalIdentifierValue);
                return;
            }

            if 
            (
                !string.Equals(
                    patch.ResourceIdentifier.SchemaIdentifier, 
                    SchemaIdentifiers.WindowsAzureActiveDirectoryGroup, 
                    StringComparison.Ordinal)
            )
            {
                return;
            }

            operation =
                patchRequest
                .Operations
                .SingleOrDefault(
                    (PatchOperation item) =>
                            item.Path != null
                        &&  string.Equals(item.Path.AttributePath, AttributeNames.Members, StringComparison.Ordinal));
            if (null == operation)
            {
                return;
            }

            await this.UpdateMembers(patch.ResourceIdentifier, operation, correlationIdentifier);
        }

        private async Task UpdateExternalIdentifier(
            IResourceIdentifier resourceIdentifier,
            string correlationIdentifier,
            string value = null)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResourceIdentifier);
            }

            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                switch (resourceIdentifier.SchemaIdentifier)
                {
                    case SchemaIdentifiers.Core2EnterpriseUser:
                        Amazon.IdentityManagement.Model.User user =
                            await this.RetrieveUser(resourceIdentifier.Identifier, proxy);
                        if (null == user)
                        {
                            return;
                        }

                        UpdateUserRequest updateUserRequest =
                            new UpdateUserRequest(user.UserName)
                            {
                                NewUserName = value
                            };

                        await proxy.UpdateUserAsync(updateUserRequest);
                        break;

                    case SchemaIdentifiers.WindowsAzureActiveDirectoryGroup:
                        Group group =
                            await this.RetrieveGroup(resourceIdentifier.Identifier, proxy);
                        if (null == group)
                        {
                            return;
                        }

                        UpdateGroupRequest updateGroupRequest =
                            new UpdateGroupRequest(group.GroupName)
                            {
                                NewGroupName = group.GroupName
                            };

                        await proxy.UpdateGroupAsync(updateGroupRequest);
                        break;

                    default:
                        throw new NotSupportedException(resourceIdentifier.SchemaIdentifier);
                }
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        private async Task UpdateMembers(
            IResourceIdentifier resourceIdentifier,
            PatchOperation membershipOperation,
            string correlationIdentifier)
        {
            if (null == resourceIdentifier)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameResourceIdentifier);
            }

            if (null == membershipOperation)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameMembershipOperation);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameCorrelationIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.Identifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResourceIdentifier);
            }

            if (string.IsNullOrWhiteSpace(resourceIdentifier.SchemaIdentifier))
            {
                throw new ArgumentException(ProvisioningAgentResources.ExceptionInvalidResourceIdentifier);
            }

            IAmazonIdentityManagementService proxy = null;
            try
            {
                proxy = AWSClientFactory.CreateAmazonIdentityManagementServiceClient(this.credentials);

                Group group = await this.RetrieveGroup(resourceIdentifier.Identifier, proxy);
                if (null == group)
                {
                    string warning =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        AmazonProvisioningAgentResources.WarningEntryNotFoundTemplate,
                        typeof(Group).Name,
                        resourceIdentifier.Identifier);
                    ProvisioningAgentMonitor.Instance.Warn(warning, correlationIdentifier);
                    return;
                }

                switch (membershipOperation.Name)
                {
                    case OperationName.Add:
                        foreach (OperationValue value in membershipOperation.Value)
                        {
                            await this.AddMember(group.GroupName, value.Value, proxy, correlationIdentifier);
                        }
                        break;
                    case OperationName.Remove:
                        foreach (OperationValue value in membershipOperation.Value)
                        {
                            await this.RemoveMember(group.GroupName, value.Value, proxy, correlationIdentifier);
                        }
                        break;
                    default:
                        string unsupportedOperation = Enum.GetName(typeof(OperationName), membershipOperation.Name);
                        throw new NotSupportedException(unsupportedOperation);
                }
            }
            finally
            {
                if (proxy != null)
                {
                    proxy.Dispose();
                    proxy = null;
                }
            }
        }

        private static void Validate(IRetrievalParameters parameters)
        {
            if (null == parameters)
            {
                throw new ArgumentNullException(AmazonWebServicesProvider.ArgumentNameParameters);
            }


            if (!parameters.SchemaIdentifier.Equals(SchemaIdentifiers.WindowsAzureActiveDirectoryGroup, StringComparison.Ordinal))
            {
                return;
            }
            
            if
            (
                    parameters.RequestedAttributePaths != null
                &&  parameters
                     .RequestedAttributePaths
                    .Any(
                        (string item) =>
                            string.Equals(item, AttributeNames.Members))
            )
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionRetrievingGroupMembersNotSupported);
            }

            if
            (
                    (
                            null == parameters.RequestedAttributePaths
                        ||  !parameters.RequestedAttributePaths.Any()
                    )
                && !parameters
                    .ExcludedAttributePaths
                    .Any(
                        (string item) =>
                            string.Equals(item, AttributeNames.Members))
            )
            {
                throw new NotSupportedException(ProvisioningAgentResources.ExceptionRetrievingGroupMembersNotSupported);
            }
        }

        private class NoAuthentication: AuthenticationOptions
        {
            private const string AuthenticationTypeValue = "None";

            public NoAuthentication()
                : base(NoAuthentication.AuthenticationTypeValue)
            {
            }
        }
    }
}
