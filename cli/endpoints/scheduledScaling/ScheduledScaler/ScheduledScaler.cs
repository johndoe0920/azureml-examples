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

            logger.LogInformation($"Fetching schedule file");
            dynamic scheduleJson = ScheduleHelper.ReadScheduleJson(
                config["AzureStorageConnectionString"], config["ContainerName"], config["ScheduleFileName"]);

            logger.LogInformation($"Fetching scale profile");
            var scaleProfile = ScheduleHelper.GetScaleProfile(scheduleJson, logger);
            logger.LogInformation($"Scale profile to use is {scaleProfile}");

            var accessToken = OnlineEndpointsHelper.GetToken(config, logger).Result;
            logger.LogInformation($"Getting workspace client");
            var workspaceClient = OnlineEndpointsHelper.CreateMachineLearningWorkspacesClientAsync(accessToken, config["Subscription"]);
            logger.LogInformation($"Got workspace client");
            
            logger.LogInformation($"Getting onlineDeployment");
            var onlineDeployment = OnlineEndpointsHelper.GetDeploymentResource(config, workspaceClient, logger);
            logger.LogInformation($"Got onlineDeployment");

            var changeNeeded = CompareDeploymentSettings(scaleProfile, onlineDeployment, logger); 

            if (changeNeeded){
                UpdateOnlineDeployment(config, onlineDeployment, workspaceClient, scaleProfile, logger);
                logger.LogInformation("Done Updating Deployment");
            }

        }

        static bool CompareDeploymentSettings(
            dynamic scaleProfile, 
            OnlineDeploymentTrackedResource deployment, 
            ILogger logger
        ){

            var deploymentScaleSettings = deployment.Properties.ScaleSettings as ManualScaleSettings;
            logger.LogInformation($"Current instanceCount setting: {deploymentScaleSettings.InstanceCount}");
            logger.LogInformation($"Scheduled instanceCount setting: {scaleProfile["instanceCount"].ToString()}");

            if (deploymentScaleSettings.InstanceCount != (int)scaleProfile["instanceCount"]){
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
            dynamic scaleProfile,
            ILogger logger
        ){
            await OnlineEndpointsHelper.UpdateDeploymentResource(config, onlineDeployment, workspaceClient, (int)scaleProfile["instanceCount"], logger);
        }

        
    }
}
