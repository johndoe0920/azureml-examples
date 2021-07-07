using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.MachineLearningServices;
using Microsoft.Azure.Management.MachineLearningServices.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Microsoft.Rest;

namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    /// <summary>
    /// Online endpoint client to get and update deployments.
    /// </summary>
    public class OnlineEndpointsHelper
    {
        public static OnlineDeploymentTrackedResource GetDeploymentResource(
            IConfigurationRoot config, 
            AzureMachineLearningWorkspacesClient workspaceClient,
            ILogger logger
        ){
            return workspaceClient.OnlineDeployments.Get(
                    config["EndpointName"], 
                    config["DeploymentName"], 
                    config["WorkspaceResourceGroupName"], 
                    config["WorkspaceName"]
                );
        }

        public static AzureMachineLearningWorkspacesClient CreateMachineLearningWorkspacesClientAsync(string token, string subscriptionId)
        {
            TokenCredentials credential = new TokenCredentials(token);
            AzureMachineLearningWorkspacesClient workspaceClient = new AzureMachineLearningWorkspacesClient(credential)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.azure.com"),
            };
            return workspaceClient;
        }

        public static async Task<OnlineDeploymentTrackedResource> UpdateDeploymentResource(
            IConfigurationRoot config, 
            OnlineDeploymentTrackedResource deployment,
            AzureMachineLearningWorkspacesClient workspaceClient,
            int newInstanceCount,
            ILogger logger
        ){

            try{
                var newManualScaleSettings = new ManualScaleSettings();
                var newOnlineDeployment = new OnlineDeploymentTrackedResource();
                newOnlineDeployment.Location = deployment.Location;
                newOnlineDeployment.Tags = deployment.Tags;
                newOnlineDeployment.Properties = deployment.Properties;

                newManualScaleSettings.MaxInstances = newOnlineDeployment.Properties.ScaleSettings.MaxInstances;
                newManualScaleSettings.MinInstances = newOnlineDeployment.Properties.ScaleSettings.MinInstances;
                newManualScaleSettings.InstanceCount = newInstanceCount;

                newOnlineDeployment.Properties.ScaleSettings = newManualScaleSettings;

                OnlineDeploymentTrackedResource onlineDeploymentTrackedResource = 
                    await workspaceClient.OnlineDeployments.CreateOrUpdateAsync(
                        config["EndpointName"], 
                        config["DeploymentName"], 
                        config["WorkspaceResourceGroupName"], 
                        config["WorkspaceName"], 
                        newOnlineDeployment
                    );

                return onlineDeploymentTrackedResource;
            
            } catch (Exception e){
                logger.LogInformation($"An Exception was thrown {e.Message}");
                throw new Exception();
            }
        }

        public static async Task<string> GetToken(IConfigurationRoot config, ILogger logger)
        {
            // Read access token from config
            var accessToken = config["AuthToken"];
            logger.LogInformation("Fetching access token");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                // Get access token
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com");
            }

            logger.LogInformation("Successfully retrieved a token");
            return accessToken;
        }
    }
}
