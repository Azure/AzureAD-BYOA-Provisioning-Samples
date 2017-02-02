//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;

    public class Row: IRow
    {
        public Row(string key, IReadOnlyDictionary<string, string> columns)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (null == columns)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            this.Key = key;
            this.Columns = columns;
        }

        public IReadOnlyDictionary<string, string> Columns 
        { 
            get; 
            private set; 
        }

        public string Key
        {
            get;
            private set;
        }        
    }
}
