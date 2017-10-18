﻿using OrpheusInterfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace OrpheusSQLDDLHelper
{
    /// <summary>
    /// SQL Server definition of DDL helper.
    /// DDL helper is used to execute DB engine specific DDL commands.
    /// </summary>
    public class OrpheusSQLServerDDLHelper : IOrpheusDDLHelper
    {
        private Dictionary<Type,string> typeMap = new Dictionary<Type,string>();
        private Dictionary<int, string> dbTypeMap = new Dictionary<int, string>();
        private string databaseName;

        private IDbCommand selectSchemaObjectQuery;
        private SqlConnection secondConnection;
        private IOrpheusDatabase db;

        private void initializeTypeMap()
        {
            typeMap[typeof(byte)] = "TINYINT";
            typeMap[typeof(sbyte)] = "SMALLINT";
            typeMap[typeof(short)] = "SMALLINT";
            typeMap[typeof(ushort)] = "INT";
            typeMap[typeof(int)] = "INT";
            typeMap[typeof(uint)] = "BIGINT";
            typeMap[typeof(long)] = "BIGINT";
            typeMap[typeof(ulong)] = "BIGINT";
            typeMap[typeof(float)] = "REAL";
            typeMap[typeof(double)] = "FLOAT";
            typeMap[typeof(decimal)] = "DECIMAL";
            typeMap[typeof(bool)] = "BIT";
            typeMap[typeof(string)] = "NVARCHAR";
            typeMap[typeof(char)] = "NCHAR";
            typeMap[typeof(Guid)] = "UNIQUEIDENTIFIER";
            typeMap[typeof(DateTime)] = "DATETIME";
            typeMap[typeof(DateTimeOffset)] = "DATETIMEOFFSET";
            typeMap[typeof(byte[])] = "VARBINARY(MAX)";

            //nullable types.
            typeMap[typeof(byte?)] = "TINYINT";
            typeMap[typeof(sbyte?)] = "SMALLINT";
            typeMap[typeof(short?)] = "SMALLINT";
            typeMap[typeof(ushort?)] = "INT";
            typeMap[typeof(int?)] = "INT";
            typeMap[typeof(uint?)] = "BIGINT";
            typeMap[typeof(long?)] = "BIGINT";
            typeMap[typeof(ulong?)] = "BIGINT";
            typeMap[typeof(float?)] = "REAL";
            typeMap[typeof(double?)] = "FLOAT";
            typeMap[typeof(decimal?)] = "DECIMAL";
            typeMap[typeof(bool?)] = "BIT";
            typeMap[typeof(char?)] = "NCHAR";
            typeMap[typeof(Guid?)] = "UNIQUEIDENTIFIER";
            typeMap[typeof(DateTime?)] = "DATETIME";
            typeMap[typeof(DateTimeOffset?)] = "DATETIMEOFFSET";

            dbTypeMap[(int)DbType.Byte] = "TINYINT";
            dbTypeMap[(int)DbType.Int16] = "SMALLINT";
            dbTypeMap[(int)DbType.Int32] = "INT";
            dbTypeMap[(int)DbType.Int64] = "BIGINT";
            dbTypeMap[(int)DbType.Double] = "FLOAT";
            dbTypeMap[(int)DbType.Single] = "REAL";
            dbTypeMap[(int)DbType.Decimal] = "DECIMAL";
            dbTypeMap[(int)DbType.Boolean] = "BIT";
            dbTypeMap[(int)DbType.String] = "NVARCHAR";
            dbTypeMap[(int)DbType.AnsiString] = "VARCHAR";
            dbTypeMap[(int)DbType.AnsiStringFixedLength] = "CHAR";
            dbTypeMap[(int)DbType.Guid] = "UNIQUEIDENTIFIER";
            dbTypeMap[(int)DbType.DateTime] = "DATETIME";
            dbTypeMap[(int)DbType.DateTimeOffset] = "DATETIMEOFFSET";
            dbTypeMap[(int)DbType.Binary] = "VARBINARY(MAX)";
            dbTypeMap[(int)DbType.Time] = "TIME";

            dbTypeMap[(int)ExtendedDbTypes.StringBlob] = "NTEXT";
        }

        /// <summary>
        /// Returns true if the DBEngine supports natively the Guid type.
        /// </summary>
        /// <returns>True if the DBEngine supports natively the Guid type</returns>
        public bool SupportsGuidType { get; private set; }

        /// <summary>
        /// Returns true if a database is successfully created using the underlying db engine settings.
        /// </summary>
        /// <returns>True if database was created successfully</returns>
        public bool CreateDatabase()
        {
            var result = false;
            SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(this.db.ConnectionString);
            var dbName = connStringBuilder.InitialCatalog;
            if (this.secondConnection != null && !this.DatabaseExists(dbName))
            {
                this.secondConnection.Open();
                try
                {
                    using (var cmd = this.secondConnection.CreateCommand())
                    {
                        cmd.CommandText = String.Format("CREATE DATABASE {0}", dbName);
                        try
                        {
                            cmd.ExecuteNonQuery();
                            result = true;
                        }
                        catch
                        {
                            result = false;
                            throw;
                        }
                    }
                }
                finally
                {
                    this.secondConnection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if a database is successfully created using the underlying db engine settings.
        /// </summary>
        /// <param name="dbName">Database name</param>
        /// <returns>True if the database was created successfully</returns>
        public bool CreateDatabase(string dbName)
        {
            var result = false;
            if (this.secondConnection != null && !this.DatabaseExists(dbName))
            {
                this.secondConnection.Open();
                try {
                    using (var cmd = this.secondConnection.CreateCommand())
                    {
                        cmd.CommandText = String.Format("CREATE DATABASE {0}", dbName);
                        try
                        {
                            result = cmd.ExecuteNonQuery() > 0;
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    this.secondConnection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if a database is successfully created using the passed DDL script.
        /// </summary>
        /// <param name="ddlString">DDL command</param>
        /// <returns>True if the database was created successfully</returns>
        public bool CreateDatabaseWithDDL(string ddlString)
        {
            var result = false;
            if (this.secondConnection != null)
            {
                this.secondConnection.Open();
                try
                {
                    using (var cmd = this.secondConnection.CreateCommand())
                    {
                        cmd.CommandText = ddlString;
                        try
                        {
                            result = cmd.ExecuteNonQuery() > 0;
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    this.secondConnection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true the database exists.
        /// </summary>
        /// <param name="dbName">Database name</param>
        /// <returns>True if the database exists</returns>
        public bool DatabaseExists(string dbName)
        {
            var result = false;
            if (this.secondConnection != null)
            {
                this.secondConnection.Open();
                try {
                    using (var cmd = this.secondConnection.CreateCommand())
                    {
                        cmd.CommandText = String.Format("SELECT DATABASE_ID FROM SYS.DATABASES WHERE NAME ='{0}'", dbName);
                        try
                        {
                            IDataReader reader = cmd.ExecuteReader();
                            if (reader.Read())
                            {
                                result = reader.GetValue(0) != null;
                            }
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    this.secondConnection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if the schema object exists in the database. A schema object can be a table,view,primary key, stored procedure, etc.
        /// </summary>
        /// <param name="schemaObjectName">Schema object name</param>
        /// <returns>True if the object exists</returns>
        public bool SchemaObjectExists(string schemaObjectName)
        {
            var result = false;
            if (this.secondConnection != null)
            {
                if (this.selectSchemaObjectQuery == null)
                {
                    this.selectSchemaObjectQuery = this.secondConnection.CreateCommand();// this.DB.CreatePreparedQuery("SELECT OBJECT_ID FROM SYS.OBJECTS WHERE NAME = @NAME");
                    this.selectSchemaObjectQuery.CommandText = String.Format("SELECT OBJECT_ID FROM {0}.SYS.OBJECTS WHERE NAME = @NAME",this.DatabaseName);
                    var param = this.selectSchemaObjectQuery.CreateParameter();
                    param.ParameterName = "@NAME";
                    this.selectSchemaObjectQuery.Parameters.Add(param);
                }
                this.secondConnection.Open();
                IDataReader reader = null;
                try
                {
                    ((IDataParameter)this.selectSchemaObjectQuery.Parameters["@NAME"]).Value = schemaObjectName;
                    reader = this.selectSchemaObjectQuery.ExecuteReader();
                    if (reader.Read())
                    {
                        result = reader.GetValue(0) != null;
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                    this.secondConnection.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Database for the DDL helper.
        /// </summary>
        /// <returns>Database the helper is associated with</returns>
        public IOrpheusDatabase DB
        {
            get { return this.db; }
            set
            {
                this.db = value;
                if(this.db != null && this.secondConnection == null)
                {
                    SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(this.db.ConnectionString);
                    connStringBuilder.InitialCatalog = "master";
                    this.secondConnection = new SqlConnection(connStringBuilder.ConnectionString);
                    try
                    {
                        this.secondConnection.Open();
                        this.secondConnection.Close();
                    }
                    catch
                    {
                        this.secondConnection.Dispose();
                        this.secondConnection = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the db engine specific string equivalent, for a .net type
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>String value for the mapped DbType</returns>
        public string TypeToString(Type type)
        {
            if (this.typeMap.ContainsKey(type))
            {
               return this.typeMap[type];
            }

            return null;
        }

        /// <summary>
        /// Returns the db engine specific string equivalent, for a DbType enumeration.
        /// </summary>
        /// <param name="dataType">DbType</param>
        /// <returns>String value for the DbType</returns>
        public string DbTypeToString(DbType dataType)
        {
            if (this.dbTypeMap.ContainsKey((int)dataType))
            {
                return this.dbTypeMap[(int)dataType];
            }

            return null;
        }

        /// <summary>
        /// Identifiers that do not comply with all of the rules for identifiers must be delimited in a SQL statement, enclosed in the DelimitedIdentifier char.
        /// </summary>
        /// <returns>Char</returns>
        public char DelimitedIndetifierStart { get { return '['; } }

        /// <summary>
        /// Identifiers that do not comply with all of the rules for identifiers must be delimited in a SQL statement, enclosed in the DelimitedIdentifier char.
        /// </summary>
        /// <returns>Char</returns>
        public char DelimitedIndetifierEnd { get { return ']'; } }

        /// <summary>
        /// Properly formats a field name, to be used in a SQL statement, in case the field name is a reserved word.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string SafeFormatField(string fieldName) {  return String.Format("{0}{1}{2}",this.DelimitedIndetifierStart,fieldName,this.DelimitedIndetifierEnd);}

        /// <summary>
        /// Returns the DB specific modify table command.
        /// </summary>
        public string ModifyColumnCommand { get { return " ALTER COLUMN "; } }

        /// <summary>
        /// Properly formats an ALTER TABLE DROP COLUMN command for the underlying database engine.
        /// </summary>
        /// <param name="tableName">Table's name that schema is going to change</param>
        /// <param name="columnsToDelete">Columns for deletion</param>
        /// <returns></returns>
        public string SafeFormatAlterTableDropColumn(string tableName, List<string> columnsToDelete)
        {
            return String.Format("ALTER TABLE {0} DROP COLUMN {1}", tableName, string.Join(",", columnsToDelete.ToArray()));
        }

        /// <summary>
        /// Properly formats an ALTER TABLE ADD COLUMN command for the underlying database engine.
        /// </summary>
        /// <param name="tableName">Table's name that schema is going to change</param>
        /// <param name="columnsToAdd">Columns for creation</param>
        /// <returns></returns>
        public string SafeFormatAlterTableAddColumn(string tableName, List<string> columnsToAdd)
        {
            return String.Format("ALTER TABLE {0} ADD {1}", tableName, string.Join(",", columnsToAdd.ToArray()));
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                if (databaseName == null)
                {
                    var builder = new SqlConnectionStringBuilder(this.DB.ConnectionString);
                    databaseName =  builder.InitialCatalog;
                }
                return databaseName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public OrpheusSQLServerDDLHelper()
        {
            this.initializeTypeMap();
            this.SupportsGuidType = true;
        }
    }
}
