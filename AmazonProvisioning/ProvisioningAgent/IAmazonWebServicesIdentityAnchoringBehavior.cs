//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Threading.Tasks;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;

    public interface IAmazonWebServicesIdentityAnchoringBehavior
    {
        string Identify(Group group);
        string Identify(User user);

        Task<Group> RetrieveGroup(
            string identifier,
            IAmazonIdentityManagementService proxy);
        
        Task<User> RetrieveUser(
            string identifier,
            IAmazonIdentityManagementService proxy);
    }
}
