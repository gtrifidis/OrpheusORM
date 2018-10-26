﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OrpheusCore.ServiceProvider;
using System.IO;

namespace OrpheusCore.Configuration
{
    /// <summary>
    /// Orpheus configuration manager.
    /// </summary>
    public static class ConfigurationManager
    {
        private static IConfiguration configurationInstance;

        /// <summary>
        /// Initializes the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="services">The services.</param>
        public static void InitializeConfiguration(IConfiguration configuration, IServiceCollection services = null)
        {
            configurationInstance = configuration;
            OrpheusServiceProvider.InitializeServiceProvider(services);
        }

        /// <summary>
        /// Initializes the configuration.
        /// </summary>
        /// <param name="configurationFile">The configuration file.</param>
        public static void InitializeConfiguration(string configurationFile)
        {
            try
            {
                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.SetBasePath(Path.GetDirectoryName(configurationFile));
                configurationBuilder.AddJsonFile(configurationFile, optional: false, reloadOnChange: true);
                InitializeConfiguration(configurationBuilder.Build());
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="configurationFile">The configuration file.</param>
        public static void SaveConfiguration(string configurationFile)
        {
            File.WriteAllText(configurationFile,JsonConvert.SerializeObject(configurationInstance.Get<OrpheusConfiguration>()));
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public static OrpheusConfiguration Configuration
        {
            get
            {
                return ConfigurationInstance.Get<OrpheusConfiguration>();
            }
        }

        /// <summary>
        /// Gets the configuration instance.
        /// </summary>
        /// <value>
        /// The configuration instance.
        /// </value>
        public static IConfiguration ConfigurationInstance { get { return configurationInstance; } }
    }
}
