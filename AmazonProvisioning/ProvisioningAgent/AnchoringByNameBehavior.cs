//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Threading.Tasks;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;

    public class AnchoringByNameBehavior: IAmazonWebServicesIdentityAnchoringBehavior
    {
        private const string ArgumentNameGroup = "group";
        private const string ArgumentNameIdentifier = "identifier";
        private const string ArgumentNameProxy = "proxy";
        private const string ArgumentNameUser = "user";        
        
        public string Identify(Group group)
        {
            if (null == group)
            {
                throw new ArgumentNullException(AnchoringByNameBehavior.ArgumentNameGroup);
            }

            return group.GroupName;
        }

        public string Identify(User user)
        {
            if (null == user)
            {
                throw new ArgumentNullException(AnchoringByNameBehavior.ArgumentNameUser);
            }

            return user.UserName;
        }

        public async Task<Group> RetrieveGroup(
            string identifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (null == identifier)
            {
                throw new ArgumentException(AnchoringByNameBehavior.ArgumentNameIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AnchoringByNameBehavior.ArgumentNameProxy);
            }

            GetGroupRequest request = new GetGroupRequest(identifier);
            GetGroupResult response = await proxy.GetGroupAsync(request);
            Group result = response.Group;
            return result;
        }

        public async Task<User> RetrieveUser(
            string identifier, 
            IAmazonIdentityManagementService proxy)
        {
            if (null == identifier)
            {
                throw new ArgumentException(AnchoringByNameBehavior.ArgumentNameIdentifier);
            }

            if (null == proxy)
            {
                throw new ArgumentNullException(AnchoringByNameBehavior.ArgumentNameProxy);
            }

            GetUserRequest request =
                new GetUserRequest()
                {
                    UserName = identifier
                };
            GetUserResult response = await proxy.GetUserAsync(request);
            User result = response.User;
            return result;
        }
    }
}
