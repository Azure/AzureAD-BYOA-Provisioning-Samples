//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class DynamicResourceColumnsFactory: ColumnsFactory<DynamicResource>
    {
        protected DynamicResourceColumnsFactory(DynamicResource resource, string schema)
            :base(resource)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                throw new ArgumentNullException(nameof(schema));
            }

            this.Schema = schema;
        }

        private string Schema
        {
            get;
            set;
        }

        public override IReadOnlyDictionary<string, string> CreateColumns()
        {
            Dictionary<string, string> result =
                this
                .Resource
                .Attributes
                .ToDictionary(
                    (DynamicProperty keyItem) =>
                        keyItem.Scheme.Name,
                    (DynamicProperty valueItem) =>
                        valueItem.Values.FirstOrDefault());
            result.Add(AttributeNames.Schemas, this.Schema);
            return result;
        }     
    }
}
