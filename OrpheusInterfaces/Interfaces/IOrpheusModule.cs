﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrpheusInterfaces
{
    /// <summary>
    /// OrpheusModule represents a logical division and grouping of a set of tables.
    /// For example you can an OrdersModule, which will be comprised from many different tables.
    /// Orders,Customers,OrderLines etc. When you Save from the module level, all pending records in tables that belong to the module,
    /// will be saved as well. All master-detail relationships and keys will be updated automatically.
    /// </summary>
    public interface IOrpheusModule
    {
        /// <summary>
        /// Module's definition.
        /// </summary>
        IOrpheusModuleDefinition Definition { get; }
        
        /// <summary>
        /// Module's database.
        /// </summary>
        IOrpheusDatabase Database { get; }
        
        /// <summary>
        /// List of module's tables.
        /// </summary>
        List<IOrpheusTable> Tables { get; }
        
        /// <summary>
        /// List of module's reference tables. Reference tables are tables that are referenced from a module table, through a foreign key constraint.
        /// </summary>
        List<IOrpheusTable> ReferenceTables { get; }
        
        /// <summary>
        /// Saves all changes to the database.
        /// </summary>
        void Save();
        
        /// <summary>
        /// Loads a module's record from the database.
        /// </summary>
        /// <param name="keyValues"></param>
        void Load(List<object> keyValues = null);

        /// Loads a module's record from the database.
        /// You can configure having multiple fields and multiple values per field.
        /// Multiple field values are bound with a logical OR.
        /// Multiple fields are bound with a logical AND
        /// </summary>
        /// <param name="keyValues"></param>
        void Load(Dictionary<string, List<object>> keyValues);

        /// <summary>
        /// The module's main table.
        /// </summary>
        IOrpheusTable MainTable { get; set; }

        /// <summary>
        /// Gets a table by index, for a model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IOrpheusTable<T> GetTable<T>(int index);

        /// <summary>
        /// Gets a table by name, for a model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        IOrpheusTable<T> GetTable<T>(string tableName);

        /// <summary>
        /// Gets a table by model. Uses the model class name as the table name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IOrpheusTable<T> GetTable<T>();

        /// <summary>
        /// Gets a table by index, for a model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        IOrpheusTable<T> GetReferenceTable<T>(int index);

        /// <summary>
        /// Gets a table by name, for a model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        IOrpheusTable<T> GetReferenceTable<T>(string tableName);

        /// <summary>
        /// Gets a table by model. Uses the model class name as the table name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IOrpheusTable<T> GetReferenceTable<T>();
    }
}