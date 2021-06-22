using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                    .SetBasePath(appDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("recipe.settings.json", optional: false, reloadOnChange: true)
                    // .AddEnvironmentVariables()
                    .Build();

            // log.LogInformation(JsonConvert.SerializeObject(SerializeConfig(config)));

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

        public static string GetResourceID(IConfigurationRoot config){
            return $"subscriptions/{config["Subscription"]}/resourceGroups/{config["ResourceGroup"]}/providers/Microsoft.MachineLearningServices/workspaces/{config["Workspace"]}/onlineEndpoints/{config["Endpoint"]}/deployments/{config["Deployment"]}";
        }
    }
}