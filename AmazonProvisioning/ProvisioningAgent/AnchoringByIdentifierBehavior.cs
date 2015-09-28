//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;

    public class AnchoringByIdentifierBehavior: IAmazonWebServicesIdentityAnchoringBehavior
    {
        private const string ArgumentNameGroup = "group";
        private const string ArgumentNameIdentifier = "identifier";
        private const string ArgumentNameProxy = "proxy";
        private const string ArgumentNameUser = "user";        
        
        private const int SizeListPage = 100;

        public string Identify(Group group)
        {
            if (null == group)
            {
                throw new ArgumentNullException(AnchoringByIdentifierBehavior.ArgumentNameGroup);
            }

            return group.GroupId;
        }

        public string Identify(Amazon.IdentityManagement.Model.User user)
        {
            if (null == user)
            {
                throw new ArgumentNullException(AnchoringByIdentifierBehavior.ArgumentNameUser);
            }

            return user.UserId;
        }

        public async Task<Group> RetrieveGroup(
            string identifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (null == identifier)
            {
                throw new ArgumentException(AnchoringByIdentifierBehavior.ArgumentNameIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AnchoringByIdentifierBehavior.ArgumentNameProxy);
            }

            ListGroupsRequest request = new ListGroupsRequest();
            request.MaxItems = AnchoringByIdentifierBehavior.SizeListPage;
            while (true)
            {
                ListGroupsResponse response = await proxy.ListGroupsAsync(request);
                if (null == response.Groups || !response.Groups.Any())
                {
                    return null;
                }

                Group group =
                    response
                    .Groups
                    .SingleOrDefault(
                        (Group item) =>
                            string.Equals(item.GroupId, identifier, StringComparison.OrdinalIgnoreCase));
                if (group != null)
                {
                    return group;
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

        public async Task<Amazon.IdentityManagement.Model.User> RetrieveUser(
            string identifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (null == identifier)
            {
                throw new ArgumentException(AnchoringByIdentifierBehavior.ArgumentNameIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AnchoringByIdentifierBehavior.ArgumentNameProxy);
            }

            ListUsersRequest request = new ListUsersRequest();
            request.MaxItems = AnchoringByIdentifierBehavior.SizeListPage;
            while (true)
            {
                ListUsersResponse response = await proxy.ListUsersAsync(request);
                if (null == response.Users || !response.Users.Any())
                {
                    return null;
                }

                Amazon.IdentityManagement.Model.User result =
                    response
                    .Users
                    .SingleOrDefault(
                        (Amazon.IdentityManagement.Model.User item) =>
                            string.Equals(item.UserId, identifier, StringComparison.OrdinalIgnoreCase));
                if (result != null)
                {
                    return result;
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
    }
}
