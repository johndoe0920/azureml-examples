using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    /// <summary>
    /// Helper to load configuration settings.
    /// </summary>
    public class ConfigurationHelper
    {
        public static IConfigurationRoot GetConfiguration(string appDirectory, ILogger log)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
        
        private static JToken SerializeConfig(IConfiguration config)
        {
            JObject obj = new JObject();
            foreach (var child in config.GetChildren())
            {
                obj.Add(child.Key, SerializeConfig(child));
            }

            if (!obj.HasValues && config is IConfigurationSection section)
                return new JValue(section.Value);

            return obj;
        }

        public static bool VerifyConfigValues(
            IConfigurationRoot config,
            ILogger logger
        ){
            VerifyConfigValue(config, "SubscriptionID", logger);
            VerifyConfigValue(config, "WorkspaceResourceGroupName", logger);
            VerifyConfigValue(config, "WorkspaceName", logger);
            VerifyConfigValue(config, "EndpointName", logger);
            VerifyConfigValue(config, "DeploymentName", logger);
            VerifyConfigValue(config, "ContainerName", logger);
            VerifyConfigValue(config, "ScheduleFileName", logger);
            return true;
        }

        public static bool VerifyConfigValue(
            IConfigurationRoot config,
            string configKey,
            ILogger logger
        ){
            if(String.IsNullOrEmpty(config[configKey])){
                throw new ArgumentNullException($"{configKey} is not set. Please make sure this variable is set to ensure the function runs properly.");
            }
            return true;
        }
    }
}