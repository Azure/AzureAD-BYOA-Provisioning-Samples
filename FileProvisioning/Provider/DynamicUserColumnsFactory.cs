//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class DynamicUserColumnsFactory: DynamicResourceColumnsFactory
    {
        public DynamicUserColumnsFactory(DynamicResource resource)
            :base(resource, DynamicConstants.SchemaIdentifierUser)
        {
        }
    }
}
