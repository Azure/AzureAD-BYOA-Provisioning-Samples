//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class MemberColumnsFactory : ColumnsFactory<Resource>
    {
        private Member groupMember;

        public MemberColumnsFactory(Resource resource, Member member)
            : base(resource)
        {
            if (null == member)
            {
                throw new ArgumentNullException(nameof(member));
            }

            this.groupMember = member;
        }

        public override IReadOnlyDictionary<string, string> CreateColumns()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(3);
            result.Add(AttributeNames.Identifier, this.Resource.Identifier);
            result.Add(AttributeNames.Members, this.groupMember.Value);
            result.Add(AttributeNames.Schemas, SchemaIdentifiers.WindowsAzureActiveDirectoryGroup);
            return result;
        }     
    }
}
