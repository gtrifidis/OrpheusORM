﻿using OrpheusInterfaces.Core;
using System;

namespace OrpheusCore
{
    /// <summary>
    /// 
    /// </summary>
    public class OrpheusTableKeyField : IOrpheusTableKeyField
    {
        /// <summary>
        /// True if the underlying db engine is going to generate the value for the key.
        /// </summary>
        public bool IsDBGenerated { get; set; }
        /// <summary>
        /// Name of the field that is the table key.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Function that returns a SQL string to used to select the new key value after an insert.
        /// </summary>        
        public Func<string> KeySQLUpdate { get; set; }

        /// <summary>
        /// If set to true and the type is System.Guid then with every new insert, a value will be
        /// auto generated.
        /// </summary>
        public bool IsAutoGenerated { get; set; }

    }
}
