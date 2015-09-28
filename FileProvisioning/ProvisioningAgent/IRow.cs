//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Collections.Generic;
    
    public interface IRow
    {
        IReadOnlyDictionary<string, string> Columns { get; }
        string Key { get; }        
    }
}
