//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class DynamicUserFactory: ResourceFactory<DynamicUser>
    {
        public DynamicUserFactory(IRow row)
            :base(row)
        {
        }

        public override DynamicUser ConstructResource()
        {
            DynamicUser result = new DynamicUser();
            return result;
        }

        public override void Initialize(DynamicUser resource)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (null == this.Row.Columns)
            {
                return;
            }

            IReadOnlyCollection<DynamicProperty> attributes =
                this
                .Row
                .Columns
                .Where(
                    (KeyValuePair<string, string> item) =>
                        !string.IsNullOrWhiteSpace(item.Value))
                .Select(
                    (KeyValuePair<string, string> item) => 
                        new DynamicProperty(item.Key, item.Value))
                .ToArray();
            foreach (DynamicProperty attribute in attributes)
            {
                resource.AddAttribute(attribute);
            }
        }     
    }
}
