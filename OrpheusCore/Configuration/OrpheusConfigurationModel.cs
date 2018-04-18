﻿using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OrpheusInterfaces.Interfaces;
using System.Collections.Generic;

namespace OrpheusCore.Configuration
{
    /// <summary>
    /// Service DI configuration item.
    /// </summary>
    public class ServiceProviderItem
    {
        /// <summary>
        /// Fully qualified name for the service implementation.
        /// </summary>
        public string Implementation { get; set; }

        /// <summary>
        /// Fully qualified name for the service interface.
        /// </summary>
        public string Service { get; set; }
        
        /// <summary>
        /// Service life time.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceLifetime ServiceLifetime { get; set; }

        /// <summary>
        /// Obsolete, to be removed.
        /// </summary>
        public List<string> ConstructorParameters { get; set; }

        /// <summary>
        /// Creates a ServiceProviderItem.
        /// </summary>
        public ServiceProviderItem()
        {
            this.ServiceLifetime = ServiceLifetime.Transient;
        }
    }

    /// <summary>
    /// Orpheus logging configuration.
    /// </summary>
    public class LoggingConfiguration : ILoggingConfiguration
    {
        /// <summary>
        /// Logging level.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Log file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Maximum log file size.
        /// </summary>
        public int MaxFileSize { get; set; }
    }

    /// <summary>
    /// Orpheus's configuration.
    /// </summary>
    public class OrpheusConfiguration
    {
        /// <summary>
        /// List of services.
        /// </summary>
        public List<ServiceProviderItem> Services { get; set; }

        /// <summary>
        /// Logging configuration.
        /// </summary>
        public LoggingConfiguration Logging { get; set; }

        /// <summary>
        /// Default size for string field, when creating a db schema.
        /// </summary>
        public int DefaultStringSize { get; set; }

        /// <summary>
        /// Creates an Orpheus configuration.
        /// </summary>
        public OrpheusConfiguration()
        {
            this.DefaultStringSize = 60;
        }
    }
}
