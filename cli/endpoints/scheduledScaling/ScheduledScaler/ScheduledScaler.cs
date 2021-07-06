using System.Threading.Tasks;
using Microsoft.Azure.Management.MachineLearningServices;
using Microsoft.Azure.Management.MachineLearningServices.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    public static class ScheduledScaler
    {
        [FunctionName("ScheduledScaler")]
        public static void Run(
            [TimerTrigger("0 */1 * * * *", RunOnStartup=true)]TimerInfo scalingTimer,
            ExecutionContext context,
            ILogger logger
        ){

            var config = ConfigurationHelper.GetConfiguration(context.FunctionAppDirectory, logger);
            ConfigurationHelper.VerifyConfigValues(config, logger);

            logger.LogInformation($"Fetching schedule file {config["ScheduleFileName"]} in container {config["ContainerName"]}");
            ScaleSchedule scheduleJson = ScheduleHelper.ReadScheduleJson(
                config["AzureStorageConnectionString"], config["ContainerName"], config["ScheduleFileName"]);
            logger.LogInformation("Successfully retrieved schedule file");
            
            logger.LogInformation("Determining which scale profile to use");
            var deploymentProfile = ScheduleHelper.GetScaleProfile(scheduleJson, logger);
            logger.LogInformation($"Scale profile to use is {deploymentProfile.Name}");

            var accessToken = OnlineEndpointsHelper.GetToken(config, logger).Result;
            logger.LogInformation($"Getting workspace client");
            var workspaceClient = OnlineEndpointsHelper.CreateMachineLearningWorkspacesClientAsync(accessToken, config["Subscription"]);
            logger.LogInformation($"Got workspace client");
            
            logger.LogInformation($"Getting config for deployment {config["Deployment"]}");
            var onlineDeployment = OnlineEndpointsHelper.GetDeploymentResource(config, workspaceClient, logger);
            logger.LogInformation($"Successfully retrieved config for deployment {config["Deployment"]}");

            var changeNeeded = CompareDeploymentSettings(deploymentProfile, onlineDeployment, logger);

            if (changeNeeded){
                logger.LogInformation($"Updating deployment {config["Deployment"]}");
                var result = UpdateOnlineDeployment(config, onlineDeployment, workspaceClient, deploymentProfile, logger);
                logger.LogInformation($"Successfully updated deployment {config["Deployment"]}");
            }

        }

        static bool CompareDeploymentSettings(
            DeploymentProfile deploymentProfile, 
            OnlineDeploymentTrackedResource deployment, 
            ILogger logger
        ){
            var deploymentScaleSettings = deployment.Properties.ScaleSettings as ManualScaleSettings;
            logger.LogInformation($"Current instanceCount setting: {deploymentScaleSettings.InstanceCount}");
            logger.LogInformation($"Scheduled instanceCount setting: {deploymentProfile.instanceCount}");

            if (deploymentScaleSettings.InstanceCount != deploymentProfile.instanceCount){
                logger.LogInformation("instanceCount settings don't match! Need to update deployment!");
                return true;
            }

            logger.LogInformation("instanceCount settings match, no need for changes.");
            return false;
        }

        static async Task UpdateOnlineDeployment(
            IConfigurationRoot config, 
            OnlineDeploymentTrackedResource onlineDeployment, 
            AzureMachineLearningWorkspacesClient workspaceClient,
            DeploymentProfile deploymentProfile,
            ILogger logger
        ){
            await OnlineEndpointsHelper.UpdateDeploymentResource(config, onlineDeployment, workspaceClient, deploymentProfile.instanceCount, logger);
        }

        
    }
}
