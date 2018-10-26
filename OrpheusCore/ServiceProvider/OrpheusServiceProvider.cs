﻿using Microsoft.Extensions.DependencyInjection;
using OrpheusCore.Configuration;
using OrpheusCore.SchemaBuilder;
using OrpheusInterfaces.Configuration;
using OrpheusInterfaces.Core;
using OrpheusInterfaces.Schema;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OrpheusCore.ServiceProvider
{
    /// <summary>
    /// Orpheus DI service provider.
    /// </summary>
    public class OrpheusServiceProvider
    {
        private static IServiceProvider serviceProvider;
        private static void initializeInternalOrpheusServices(IServiceCollection serviceCollection)
        {

            //data services.
            serviceCollection.AddTransient<IOrpheusTableOptions, OrpheusTableOptions>();
            serviceCollection.AddTransient<IOrpheusModuleDefinition, OrpheusModuleDefinition>();
            serviceCollection.AddTransient<IOrpheusTableKeyField, OrpheusTableKeyField>();
            serviceCollection.AddTransient<IOrpheusModule, OrpheusModule>();

            //Schema services.
            serviceCollection.AddTransient<ISchema, SchemaBuilder.Schema>();
            serviceCollection.AddTransient<ISchemaView, SchemaObjectView>();
            serviceCollection.AddTransient<ISchemaViewTable, SchemaObjectViewTable>();
            serviceCollection.AddTransient<ISchemaTable, SchemaObjectTable>();
            serviceCollection.AddTransient<ISchemaObject, SchemaObject>();
            serviceCollection.AddTransient<ISchemaJoinDefinition, SchemaJoinDefinition>();
            serviceCollection.AddTransient<ISchemaDataObject, SchemaDataObject>();

            //configuration services
            serviceCollection.AddTransient<IDatabaseConnectionConfiguration, DatabaseConnectionConfiguration>();

            serviceCollection.AddOptions();
            serviceCollection.Configure<LoggingConfiguration>(ConfigurationManager.ConfigurationInstance.GetSection("Logging"));
            serviceCollection.Configure<List<ServiceProviderItem>>(ConfigurationManager.ConfigurationInstance.GetSection("Services"));
            serviceCollection.Configure<List<DatabaseConnectionConfiguration>>(ConfigurationManager.ConfigurationInstance.GetSection("DatabaseConnections"));
            serviceCollection.Configure<OrpheusConfiguration>(ConfigurationManager.ConfigurationInstance);
        }

        /// <summary>
        /// Initializes the service provider.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void InitializeServiceProvider(IServiceCollection services = null)
        {
            //if no service collection is passed, we are in self service mode.
            //meaning all services are registered to the Orpheus internal service collection.
            if (services == null)
            {
                services = new ServiceCollection();
            }
            //initialize internal Orpheus services.
            OrpheusServiceProvider.initializeInternalOrpheusServices(services);
            //initialize services that are defined in the configuration file.
            OrpheusServiceProvider.InitializeServiceCollection(services);
            //finally create a service provider, in order to be able to resolve services upon request.
            OrpheusServiceProvider.serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        ///  Defines a mechanism for retrieving a service object; that is, an object that
        ///  provides custom support to other objects.
        /// </summary>
        public static IServiceProvider Provider
        {
            get
            {
                if(OrpheusServiceProvider.serviceProvider == null)
                    OrpheusServiceProvider.InitializeServiceProvider();
                return OrpheusServiceProvider.serviceProvider;
            }
        }

        /// <summary>
        /// Initializes the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <exception cref="Exception">Service [service name] or [implementation] could not be resolved.</exception>
        public static void InitializeServiceCollection(IServiceCollection serviceCollection)
        {
            foreach (var scItem in ConfigurationManager.Configuration.Services)
            {
                var serviceType = Type.GetType(scItem.Service);
                var implementationType = Type.GetType(scItem.Implementation);
                //if either of the service or implementation cannot be resolved, throw an error.
                if(serviceType == null || implementationType == null)
                {
                    throw new Exception($"Service {scItem.Service} or {scItem.Implementation} could not be resolved.");
                }
                switch (scItem.ServiceLifetime)
                {
                    case ServiceLifetime.Transient:
                        {
                            serviceCollection.AddTransient(serviceType, implementationType);
                            break;
                        }

                    case ServiceLifetime.Singleton:
                        {
                            serviceCollection.AddSingleton(serviceType, implementationType);
                            break;
                        }
                    case ServiceLifetime.Scoped:
                        {
                            serviceCollection.AddScoped(serviceType, implementationType);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Resolve an interface to a concrete implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>()
        {
            T result;
            try
            {
                result = OrpheusServiceProvider.Provider.GetService<T>();
            }
            catch(Exception e)
            {
                throw e;
            }
            return result;
        }

        /// <summary>
        /// Resolve an interface to a concrete implementation, with constructor parameter support.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>(object[] constructorParameters)
        {
            try
            {
                List<Type> parametersType = new List<Type>();
                List<object> parameterValues = new List<object>();
                foreach(var obj in constructorParameters)
                {
                    if (obj != null)
                    {
                        parametersType.Add(obj.GetType());
                        parameterValues.Add(obj);
                    }
                }

                T service = Resolve<T>();

                //try to find a matching constructor based on the constructor parameters.
                //if a constructor is found, then instantiate the class.
                ConstructorInfo[] constructors = service.GetType().GetConstructors();
                ConstructorInfo constructorInfo = service.GetType().GetConstructor(parametersType.ToArray());
                if(constructorInfo != null)
                {
                    return (T)constructorInfo.Invoke(parameterValues.ToArray());
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return default(T);
        }
    }
}
