﻿using OrpheusInterfaces.Core;
using System;

namespace OrpheusCore
{
    /// <summary>
    /// 
    /// </summary>
    public class OrpheusTableKeyField : IOrpheusTableKeyField
    {
        /// <value>
        /// True if the underlying db engine is going to generate the value for the key.
        /// </value>
        public bool IsDBGenerated { get; set; }
        /// <value>
        /// Name of the field that is the table key.
        /// </value>
        public string Name { get; set; }
        /// <value>
        /// Function that returns a SQL string to used to select the new key value after an insert.
        /// </value>        
        public Func<string> KeySQLUpdate { get; set; }

        /// <value>
        /// If set to true and the type is System.Guid then with every new insert, a value will be
        /// auto generated.
        /// </value>
        public bool IsAutoGenerated { get; set; }

    }
}
