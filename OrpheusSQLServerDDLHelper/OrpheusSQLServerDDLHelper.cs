﻿using OrpheusInterfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace OrpheusSQLDDLHelper
{
    /// <summary>
    /// SQL Server definition of DDL helper.
    /// DDL helper is used to execute DB engine specific DDL commands.
    /// </summary>
    public class OrpheusSQLServerDDLHelper : ISQLServerDDLHelper
    {
        private Dictionary<Type,string> typeMap = new Dictionary<Type,string>();
        private Dictionary<int, string> dbTypeMap = new Dictionary<int, string>();
        private string databaseName;
        private ISchemaObject dummySchemaObject;

        private IDbCommand selectSchemaObjectQuery;
        private IDbCommand selectNamedSchemaObjectQuery;
        private SqlConnection _secondConnection;
        private SqlConnection _masterConnection;
        private SqlConnection secondConnection
        {
            get
            {
                if (this._secondConnection == null)
                {
                    SqlConnectionStringBuilder secondConnectionString = new SqlConnectionStringBuilder(this.db.ConnectionString);
                    this._secondConnection = new SqlConnection(secondConnectionString.ConnectionString);
                }
                return this._secondConnection;
            }
        }

        private SqlConnection masterConnection
        {
            get
            {
                if (this._masterConnection == null)
                {
                    SqlConnectionStringBuilder masterConnectionString = new SqlConnectionStringBuilder(this.db.ConnectionString);
                    masterConnectionString.InitialCatalog = "master";
                    this._masterConnection = new SqlConnection(masterConnectionString.ConnectionString);
                }
                return this._masterConnection;
            }
        }
        private IOrpheusDatabase db;
        private delegate void DDLCommandCallback(IDbCommand dbCommand);
        private delegate void ErrorCallback(Exception exception);

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
        /// Executes a DDL command.
        /// </summary>
        /// <param name="ddlCommand"></param>
        /// <param name="successCallback"></param>
        /// <param name="errorCallback"></param>
        private void executeDDLCommand(string ddlCommand,bool useMasterConnection = false, DDLCommandCallback successCallback = null, ErrorCallback errorCallback = null)
        {
            var sqlConnection = useMasterConnection ? this.masterConnection : this.secondConnection;
            sqlConnection.Open();
            try
            {
                using (var cmd = sqlConnection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = ddlCommand;
                        cmd.ExecuteNonQuery();
                        if(successCallback != null)
                            successCallback(cmd);
                    }
                    catch(Exception e)
                    {
                        if (errorCallback != null)
                            errorCallback(e);
                        else
                            throw;
                    }
                }
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        private IDbCommand prepareSQLCommand(string sql, List<string> parameters = null, List<object> parameterValues = null)
        {
            var cmd = this.secondConnection.CreateCommand();
            cmd.CommandText = sql;
            if(parameters != null)
            {
                for(var i=0;i<=parameters.Count - 1; i++)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = parameters[i];
                    if(parameterValues != null)
                        param.Value = parameterValues[i];
                    cmd.Parameters.Add(param);
                }
            }
            return cmd;

        }

        private bool schemaObjectExists(string schemaObjectName, string schemaName = null)
        {
            var result = false;
            if (this.secondConnection != null)
            {
                var isNamedSchema = schemaName != null;
                if (this.selectNamedSchemaObjectQuery == null && isNamedSchema)
                {
                    //this.selectSchemaObjectQuery = this.db.CreatePreparedQuery(String.Format("SELECT OBJECT_ID FROM {0}.SYS.OBJECTS WHERE NAME = @NAME",this.DatabaseName),
                    //    new List<string>() { "@NAME" });
                    this.selectNamedSchemaObjectQuery = this.secondConnection.CreateCommand();
                    var SQL = new StringBuilder();
                    SQL.AppendFormat("SELECT OBJECT_ID,{0}.SYS.SCHEMAS.NAME FROM {1}.SYS.OBJECTS ", this.DatabaseName, this.DatabaseName);
                    SQL.Append("LEFT JOIN SYS.SCHEMAS on SYS.SCHEMAS.SCHEMA_ID = SYS.OBJECTS.SCHEMA_ID");
                    SQL.Append(" WHERE SYS.OBJECTS.NAME = @NAME");
                     SQL.Append(" AND SYS.SCHEMAS.NAME = @SCHEMA_NAME");
                    this.selectNamedSchemaObjectQuery.CommandText = SQL.ToString();

                    var param = this.selectNamedSchemaObjectQuery.CreateParameter();
                    param.ParameterName = "@NAME";

                    this.selectNamedSchemaObjectQuery.Parameters.Add(param);

                    var schemaNameParam = this.selectNamedSchemaObjectQuery.CreateParameter();
                    schemaNameParam.ParameterName = "@SCHEMA_NAME";
                    this.selectNamedSchemaObjectQuery.Parameters.Add(schemaNameParam);
                }
                if (this.selectSchemaObjectQuery == null && !isNamedSchema)
                {
                    this.selectSchemaObjectQuery = this.secondConnection.CreateCommand();
                    var SQL = new StringBuilder();
                    SQL.AppendFormat("SELECT OBJECT_ID FROM {0}.SYS.OBJECTS ", this.DatabaseName);
                    SQL.Append("WHERE SYS.OBJECTS.NAME = @NAME");

                    this.selectSchemaObjectQuery.CommandText = SQL.ToString();

                    var param = this.selectSchemaObjectQuery.CreateParameter();
                    param.ParameterName = "@NAME";

                    this.selectSchemaObjectQuery.Parameters.Add(param);
                }

                this.secondConnection.Open();
                IDataReader reader = null;
                try
                {
                    IDbCommand commandForExecution = isNamedSchema ? this.selectNamedSchemaObjectQuery : this.selectSchemaObjectQuery;
                    if (isNamedSchema)
                    {
                        ((IDataParameter)commandForExecution.Parameters["@NAME"]).Value = schemaObjectName;
                        ((IDataParameter)commandForExecution.Parameters["@SCHEMA_NAME"]).Value = schemaName;
                    }
                    else
                    {
                        ((IDataParameter)commandForExecution.Parameters["@NAME"]).Value = schemaObjectName;
                    }

                    reader = commandForExecution.ExecuteReader();
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
        /// Returns true if the DBEngine supports natively the Guid type.
        /// </summary>
        /// <returns>True if the DBEngine supports natively the Guid type</returns>
        public bool SupportsGuidType { get; private set; }

        /// <summary>
        /// Returns true if the DBEngine supports having schema name spaces. From the currently supported databases, only SQL has this feature.
        /// </summary>
        public bool SupportsSchemaNameSpace { get; private set; }

        /// <summary>
        /// Returns true if a database is successfully created using the underlying db engine settings.
        /// </summary>
        /// <returns>True if database was created successfully</returns>
        public bool CreateDatabase()
        {
            var result = false;
            SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(this.db.ConnectionString);
            var dbName = connStringBuilder.InitialCatalog;
            if(!this.DatabaseExists(dbName))
                this.executeDDLCommand(String.Format("CREATE DATABASE {0}", dbName), true, (dbCommand) => {
                    result = true;
                },(error)=> {
                    result = false;
                });
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
            if (!this.DatabaseExists(dbName))
                this.executeDDLCommand(String.Format("CREATE DATABASE {0}", dbName), true, (dbCommand) => {
                    result = true;
                }, (error) => {
                    result = false;
                });
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
            this.executeDDLCommand(ddlString, true, (dbCommand) => {
                result = true;
            }, (error) => {
                result = false;
            });
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
            this.executeDDLCommand(String.Format("SELECT DATABASE_ID FROM SYS.DATABASES WHERE NAME ='{0}'", dbName), true, (dbCommand) => {
                IDataReader reader = dbCommand.ExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetValue(0) != null;
                }
            }, (error) => {
                result = false;
            });
            return result;
        }

        /// <summary>
        /// Returns true if the schema object exists in the database. A schema object can be a table,view,primary key, stored procedure, etc.
        /// </summary>
        /// <param name="schemaObjectName"></param>
        /// <returns></returns>
        public bool SchemaObjectExists(string schemaObjectName)
        {
            return this.schemaObjectExists(schemaObjectName);
        }

        /// <summary>
        /// Returns true if the schema object exists in the database. A schema object can be a table,view,primary key, stored procedure, etc.
        /// </summary>
        /// <param name="schemaObjectName">Schema object name</param>
        /// <returns>True if the object exists</returns>
        public bool SchemaObjectExists(ISchemaObject schemaObject)
        {
            
            string objectName = null;
            if (schemaObject.Schema.Name == null)
                objectName = schemaObject.SQLName;
            else
            {
                var hasDot = schemaObject.SQLName.IndexOf(".") >= 0;
                objectName = hasDot ? schemaObject.SQLName.Split('.')[1] : schemaObject.SQLName;
            }

            return this.schemaObjectExists(objectName);
        }

        /// <summary>
        /// Returns true if the schema object exists in the database.
        /// </summary>
        /// <param name="schemaConstraint"></param>
        /// <returns></returns>
        public bool SchemaObjectExists(ISchemaConstraint schemaConstraint)
        {
            return this.schemaObjectExists(schemaConstraint.Name);
        }

        /// <summary>
        /// Returns true if the schema object exists in the database. A schema object can be a table,view,primary key, stored procedure, etc.
        /// </summary>
        /// <param name="schemaObjectName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public bool SchemaObjectExists(string schemaObjectName, string schemaName = null)
        {
            return this.schemaObjectExists(schemaObjectName, schemaName);
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
                    try
                    {
                        this.secondConnection.Open();
                        this.secondConnection.Close();
                    }
                    catch
                    {
                        this.secondConnection.Dispose();
                        this._secondConnection = null;
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
        /// Returns the underlying database engine type.
        /// </summary>
        public DatabaseEngineType DbEngineType { get; private set; }

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
            this.SupportsSchemaNameSpace = true;
            this.DbEngineType = DatabaseEngineType.dbSQLServer;
        }

        #region schema 
        /// <summary>
        /// Returns true if the schema exists.
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public bool SchemaExists(string schemaName)
        {
            var result = false;
            var cmd = this.prepareSQLCommand(String.Format("SELECT Name FROM SYS.SCHEMAS WHERE NAME = @NAME"),
                new List<string>() { "@NAME" },
                new List<object>() { schemaName });

            if (this.secondConnection.State != ConnectionState.Open)
                this.secondConnection.Open();
            IDataReader reader = cmd.ExecuteReader();
            try
            {
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
            return result;
        }

        /// <summary>
        /// Creates a schema.
        /// </summary>
        /// <param name="schemaName"></param>
        public void CreateSchema(string schemaName)
        {
            if (!this.SchemaExists(schemaName))
            {
                this.executeDDLCommand(String.Format("CREATE SCHEMA {0}", schemaName));
            }
        }

        /// <summary>
        /// Drops a schema.
        /// </summary>
        /// <param name="schemaName"></param>
        public void DropSchema(string schemaName)
        {
            if (this.SchemaExists(schemaName))
            {
                this.executeDDLCommand(String.Format("DROP SCHEMA {0}", schemaName));
            }
        }
        #endregion

        #region database role
        /// <summary>
        /// Returns true if the database role exists.
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public bool DatabaseRoleExists(string roleName)
        {
            //select * from sys.database_principals
            var result = false;
            var cmd = this.prepareSQLCommand("SELECT Name FROM SYS.DATABASE_PRINCIPALS WHERE NAME = @NAME AND TYPE = @ROLE_TYPE",
                new List<string>() { "@NAME","@ROLE_TYPE" },
                new List<object>() { roleName,"R" });

            if (this.secondConnection.State != ConnectionState.Open)
                this.secondConnection.Open();
            IDataReader reader = cmd.ExecuteReader();
            try
            {
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
            return result;

        }

        /// <summary>
        /// Creates a database role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="owner"></param>
        public void CreateDatabaseRole(string roleName, string owner = null)
        {
            var ddlString = String.Format("CREATE ROLE {0}", roleName);
            if (owner != null)
                ddlString = String.Format("{0} AUTHORIZATION {1}", owner);
            this.executeDDLCommand(ddlString);
        }

        /// <summary>
        /// Drops a database role.
        /// </summary>
        /// <param name="roleName"></param>
        public void DropDatabaseRole(string roleName)
        {
            if (this.DatabaseRoleExists(roleName))
            {
                this.executeDDLCommand(String.Format("DROP ROLE {0}",roleName));
            }
        }

        /// <summary>
        /// Adds a database user to the specified role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userName"></param>
        public void AddDatabaseRoleMember(string roleName, string userName)
        {
            if (this.DatabaseRoleExists(roleName))
            {
                this.executeDDLCommand(String.Format("ALTER ROLE {0} ADD MEMBER {1}", roleName, userName));
            }
        }

        /// <summary>
        /// Drops a database user to the specified role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="userName"></param>
        public void DropDatabaseRoleMember(string roleName, string userName)
        {
            if (this.DatabaseRoleExists(roleName))
            {
                this.executeDDLCommand(String.Format("ALTER ROLE {0} DROP MEMBER {1}", roleName, userName));
            }
        }

        #endregion

        #region user
        /// <summary>
        /// Creates a contained database user.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public void CreateDatabaseUser(string userName, string password)
        {
            this.executeDDLCommand(String.Format("CREATE USER {0} WITH PASSWORD = '{1}'", userName, password));
        }

        /// <summary>
        /// Changes the password for an existing user.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        public void ChangeDatabaseUserPassword(string userName, string oldPassword, string newPassword)
        {
            this.executeDDLCommand(String.Format("ALTER USER {0} WITH PASSWORD = '{1} OLD_PASSWORD = '{2}'", userName, newPassword, oldPassword));
        }


        /// <summary>
        /// Drops a user.
        /// </summary>
        /// <param name="userName"></param>
        public void DropDatabaseUser(string userName)
        {
            if (this.DatabaseUserExists(userName))
            {
                this.executeDDLCommand(String.Format("DROP USER {0}", userName));
            }
        }

        /// <summary>
        /// Returns true if a database user exists.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool DatabaseUserExists(string userName)
        {
            //select * from sys.database_principals
            var result = false;
            var cmd = this.prepareSQLCommand("SELECT Name FROM SYS.DATABASE_PRINCIPALS WHERE NAME = @NAME AND TYPE = @ROLE_TYPE",
                new List<string>() { "@NAME", "@ROLE_TYPE" },
                new List<object>() { userName, "S" });

            if (this.secondConnection.State != ConnectionState.Open)
                this.secondConnection.Open();

            IDataReader reader = cmd.ExecuteReader();
            try
            {
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
            return result;
        }
        #endregion

        #region databases
        /// <summary>
        /// Enables/disables the contained database feature on the SQL server instance. A feature supported from SQL server 2012 and later.
        /// </summary>
        /// <param name="enable"></param>
        public void EnableContainedDatabases(bool enable)
        {
            SqlCommand spConfigure = new SqlCommand("sp_configure", this.secondConnection);
            spConfigure.CommandType = CommandType.StoredProcedure;
            spConfigure.Parameters.Add(new SqlParameter("@configname", SqlDbType.VarChar, 35));
            spConfigure.Parameters.Add(new SqlParameter("@configvalue", SqlDbType.Int));
            spConfigure.Parameters["@configname"].Value = "contained database authentication";
            spConfigure.Parameters["@configvalue"].Value = enable ? 1 : 0;

            SqlCommand reconfigure = this.secondConnection.CreateCommand();
            reconfigure.CommandText = "RECONFIGURE";

            try
            {
                if (this.secondConnection.State != ConnectionState.Open)
                    this.secondConnection.Open();
                spConfigure.ExecuteNonQuery();
                reconfigure.ExecuteNonQuery();
            }
            catch
            {
                throw;
            }
        }
        #endregion
    }
}
