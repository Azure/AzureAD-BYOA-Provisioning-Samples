//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class AmazonWebServicesProviderBase: ProviderBase
    {
        public abstract IAmazonWebServicesIdentityAnchoringBehavior AnchoringBehavior { get; }
    }
}
