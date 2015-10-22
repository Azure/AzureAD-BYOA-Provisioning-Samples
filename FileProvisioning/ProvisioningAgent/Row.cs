//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;

    public class Row: IRow
    {
        private const string ArgumentNameColumns = "columns";
        private const string ArgumentNameKey = "key";

        public Row(string key, IReadOnlyDictionary<string, string> columns)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(Row.ArgumentNameKey);
            }

            if (null == columns)
            {
                throw new ArgumentNullException(Row.ArgumentNameColumns);
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
