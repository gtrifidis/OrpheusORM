﻿using OrpheusInterfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.Practices.Unity;
using OrpheusAttributes;
using Microsoft.Extensions.Logging;

namespace OrpheusCore.SchemaBuilder
{
    internal class OrpheusSchemaInfo
    {
        [Length(60)]
        public string SchemaDescription { get; set; }
        public double Version { get; set; }
        [PrimaryKey]
        public Guid SchemaId { get; set; }
    }

    internal class OrpheusSchemaObject
    {
        [Length(100)]
        [PrimaryKey]
        public string ObjectName { get; set; }

        public Guid ObjectId { get; set; }

        [DataTypeAttribute((int)ExtendedDbTypes.StringBlob)]
        public string ObjectDDL { get; set; }

        public DateTime CreatedOn { get; set; }

        public int ObjectType { get; set; }
    }

    internal class OrpheusSchemaModule
    {
        [PrimaryKey]
        public string ModuleName { get; set; }
        public string ModuleDefinition { get; set; }
    }

    public class Schema : ISchema
    {
        private IOrpheusDatabase db;
        private Guid id;
        private ISchemaTable internalSchemaInfo;
        private ISchemaTable internalSchemaObject;
        private ISchemaTable internalSchemaModules;
        private string schemaObjectPrefix = "Orpheus";
        private string schemaObjectsTable = "SchemaObject";
        private string schemaInfoTable = "SchemaInfo";
        private string schemaModulesTable = "SchemaModule";
        private List<ISchemaObject> schemaObjectCache;
        private IDbCommand schemaObjectExistsPreparedQuery;
        private ILogger logger;
        private void initializeOrpheusSchema()
        {

            this.internalSchemaObject = this.AddSchemaTable(typeof(OrpheusSchemaObject));

            this.internalSchemaInfo = this.AddSchemaTable(typeof(OrpheusSchemaInfo));
            var data = new List<OrpheusSchemaInfo>();
            data.Add(new OrpheusSchemaInfo() { SchemaDescription = this.Description, Version = this.Version, SchemaId = this.id });
            this.internalSchemaInfo.SetData<OrpheusSchemaInfo>(data);

            this.internalSchemaModules = this.AddSchemaTable(typeof(OrpheusSchemaModule));
        }
        
        public IOrpheusDatabase DB { get { return this.db; } }

        public List<ISchemaObject> SchemaObjects { get; set; }

        public string Description { get; private set; }

        public double Version { get; private set; }

        public Guid Id { get { return this.id; } }
        
        /// <summary>
        /// Parameterless constructor.
        /// </summary>
        public Schema()
        {
            this.logger = OrpheusIocContainer.Resolve<ILogger>();
        }

        /// <summary>
        /// Creates an Orpheus schema.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="description"></param>
        /// <param name="version"></param>
        /// <param name="id"></param>
        public Schema(IOrpheusDatabase db,string description, double version, Guid id)
        {
            this.db = db;
            this.Description = description;
            this.Version = version;
            this.id = id;
            this.logger = OrpheusIocContainer.Resolve<ILogger>();
            this.SchemaObjects = new List<ISchemaObject>();
            this.initializeOrpheusSchema();
        }
        
        /// <summary>
        /// Creates a schema object.
        /// </summary>
        /// <param name="schemaObject"></param>
        /// <returns></returns>
        public ISchemaObject AddSchemaObject(ISchemaObject schemaObject)
        {
            if(schemaObject != null)
            {
                schemaObject.Schema = this;
                this.SchemaObjects.Add(schemaObject);
                schemaObject.AddDependency(this.internalSchemaObject);
            }
            return schemaObject;
        }

        /// <summary>
        /// Creates a schema table and initializes table-name, dependencies and generating fields from a model, if provided.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dependencies"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public ISchemaTable AddSchemaTable(string tableName, List<ISchemaObject> dependencies = null, object model = null)
        {
            var result = OrpheusIocContainer.Resolve<ISchemaTable>();
            result.SQLName = tableName;
            if(dependencies != null)
            {
                foreach (var dep in dependencies)
                    result.AddDependency(dep);
            }
            this.AddSchemaObject(result);
            if (model != null)
                result.CreateFieldsFromModel(model);

            return result;
        }

        /// <summary>
        /// Creates a schema table and initializes table-name, dependencies and generating fields from a model, if provided.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public  ISchemaTable AddSchemaTable(object model, List<ISchemaObject> dependencies = null)
        {
            return this.AddSchemaTable(model.GetType().Name, dependencies, model);
        }

        /// <summary>
        /// Creates a schema table and initializes table-name, dependencies and generating fields from a model, if provided.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public ISchemaTable AddSchemaTable(Type modelType, List<ISchemaObject> dependencies = null)
        {
            var modelInstance = Activator.CreateInstance(modelType);
            return this.AddSchemaTable(modelInstance, dependencies);
        }

        /// <summary>
        /// Removes a schema object from the schema list.
        /// </summary>
        /// <param name="schemaObject"></param>
        public void RemoveSchemaObject(ISchemaObject schemaObject)
        {
            this.SchemaObjects.Remove(schemaObject);
        }
        
        /// <summary>
        /// Iterates through registered schema objects and executes them.
        /// </summary>
        public void Execute()
        {
            this.logger.LogDebug("========== Start Creating schema {0}==========", this.Description);
            this.SchemaObjects.ForEach(schObj =>
            {
                schObj.Execute();
            });
            this.logger.LogDebug("========== End Creating schema {0}==========", this.Description);
        }

        /// <summary>
        /// Drops schema. Removes all schema objects from the database.
        /// </summary>
        public void Drop()
        {
            this.logger.LogDebug("========== Start Dropping schema {0}==========", this.Description);
            this.internalSchemaObject.Drop();
            //this.SchemaObjects.ForEach(schObj => {
            //    ////Due to dependencies it's not guaranteed that the schema objects will be drop sequentially as they are listed.
            //    ////therefore we only drop schema objects that still do exist.
            //    //if(schObj.IsCreated)
            //        schObj.Drop();
            //});
            //if the schema is dropped make sure to clear the cache.
            if (this.schemaObjectCache != null)
            {
                this.schemaObjectCache.Clear();
                this.schemaObjectCache = null;
            }
            this.logger.LogDebug("========== End Dropping schema {0}==========", this.Description);
        }

        /// <summary>
        /// Loads schema definition from a file.
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadFromFile(string fileName)
        {
            XDocument xDoc = XDocument.Load(fileName);
            var schema = from node in xDoc.Descendants("OrpheusSchema")
                         select new
                         {
                             Description = node.Element("Description").Value,
                             id = node.Element("Id").Value,
                             Version = Convert.ToDouble(node.Element("Version").Value),
                             SchemaObjects = (from schemaNode in node.Element("SchemaObject").Elements()
                                             select new {
                                                 
                                             }).ToList()
                         };

        }
        /// <summary>
        /// Saves schema definition to a file. If the file exists it will overwrite it.
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveToFile(string fileName)
        {
            XDocument xDoc = null;
            try
            {
                    xDoc = new XDocument(new XElement("OrpheusSchema",
                    new XElement("Description", this.Description)),
                    new XElement("Id",this.Id.ToString()),
                    new XElement("Version",this.Version),
                    new XElement("SchemaObject",
                        from schemaObject in this.SchemaObjects
                        select new XElement(schemaObject.SQLName,
                            new XElement("DDL",schemaObject.GetDDLString())))
                    );
                xDoc.Save(fileName);
            }
            finally
            {
                xDoc = null;
            }
        }
        /// <summary>
        /// Orpheus schema objects table.
        /// </summary>
        public string SchemaObjectsTable
        {
            get { return this.schemaObjectPrefix + this.schemaObjectsTable; }
        }
        /// <summary>
        /// Orpheus schema info table.
        /// </summary>
        public string SchemaInfoTable
        {
            get { return this.schemaObjectPrefix + this.schemaInfoTable; }
        }

        public string SchemaModulesTable
        {
            get { return this.schemaObjectPrefix + this.schemaModulesTable; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schemaObject"></param>
        /// <returns></returns>
        public Guid SchemaObjectExists(ISchemaObject schemaObject)
        {
            var result = Guid.Empty;
            //creating the schema cache and storing any schema object that is not in it.
            //caching the result to avoid hitting the database everytime.
            if (this.schemaObjectCache == null)
                this.schemaObjectCache = new List<ISchemaObject>();
            if(this.schemaObjectExistsPreparedQuery == null)
            {
                this.schemaObjectExistsPreparedQuery = this.db.CreatePreparedQuery(String.Format("SELECT ObjectId,ObjectName FROM {0} WHERE ObjectName = @OBJECT_NAME", this.SchemaObjectsTable), new List<string>() { "@OBJECT_NAME" });
            }
            var schemaInCache = this.schemaObjectCache.Find(cacheObject => { return cacheObject.SQLName.ToLowerInvariant() == schemaObject.SQLName.ToLowerInvariant(); });
            if (schemaInCache != null)
            {
                //if the item does not exist in the database but exists in object cache, then update the cache.
                if (!this.db.DDLHelper.SchemaObjectExists(schemaObject.SQLName))
                {
                    this.schemaObjectCache.Remove(schemaInCache);
                    return result;
                }
                return schemaInCache.UniqueKey;
            }
            ((IDataParameter)this.schemaObjectExistsPreparedQuery.Parameters["@OBJECT_NAME"]).Value = schemaObject.SQLName;
            IDataReader reader = null;
            try
            {
                reader = this.schemaObjectExistsPreparedQuery.ExecuteReader();
                if (reader.Read())
                {
                    var id = reader.GetGuid(0);
                    if (id != Guid.Empty)
                    {
                        result = new Guid(id.ToString());
                        var newSchemaObject = new SchemaObject();
                        newSchemaObject.Schema = this;
                        newSchemaObject.SQLName = reader.GetString(1);
                        newSchemaObject.UniqueKey = result;
                        this.schemaObjectCache.Add(newSchemaObject);
                    }
                }
            }
            catch(Exception e)
            {
                result = Guid.Empty;
                this.logger.LogWarning(e.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return result;
        }
    }
}