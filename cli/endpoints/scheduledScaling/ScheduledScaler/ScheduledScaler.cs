using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    public static class ScheduledScaler
    {
        [FunctionName("ScheduledScaler")]
        public static void Run(
            [TimerTrigger("0 */1 * * * *", RunOnStartup=true)]TimerInfo myTimer,
            ExecutionContext context,
            ILogger logger)
        {
            var config = ConfigurationHelper.GetConfiguration(context.FunctionAppDirectory, logger);
            string connectionString = config["AzureStorageConnectionString"];
            // logger.LogInformation($"Azure Storage Connection String: {connectionString}");

            dynamic scheduleJson = ReadScheduleJson(connectionString);
            var scaleProfile = GetScaleProfile(scheduleJson, logger);

            logger.LogInformation($"Scale profile to use is {scaleProfile}");

            var resourceID = ConfigurationHelper.GetResourceID(config);
            logger.LogInformation($"ResourceID: {resourceID}");

            var onlineDeployment = GetOnlineDeployment(config, resourceID, logger).Result;
            var changeNeeded = CompareDeploymentSettings(scaleProfile, onlineDeployment, logger); 

            if (changeNeeded){
                UpdateOnlineDeployment(config, resourceID, onlineDeployment, scaleProfile, logger);
            }

        }

        static bool CompareDeploymentSettings(dynamic scaleProfile, OnlineDeployment deployment, ILogger logger){

            logger.LogInformation($"Deployment properties minInstances: {deployment.properties.scaleSettings.minInstances}");
            logger.LogInformation($"Deployment properties maxInstances: {deployment.properties.scaleSettings.maxInstances}");

            var minInstances = scaleProfile["minInstances"].ToString();
            var maxInstances = scaleProfile["maxInstances"].ToString();
            logger.LogInformation($"scaleProfile minInstances: {minInstances}");
            logger.LogInformation($"scaleProfile minInstances: {maxInstances}");

            if (deployment.properties.scaleSettings.minInstances != (int)scaleProfile["minInstances"] || deployment.properties.scaleSettings.maxInstances != (int)scaleProfile["maxInstances"]){
                logger.LogInformation("Number of instances settings don't match!");
                return true;
            } else {
                logger.LogInformation("All good here");
            }

            return false;
        }

        static async Task UpdateOnlineDeployment(
            IConfigurationRoot config, 
            string targetResourceId, 
            OnlineDeployment onlineDeployment, 
            dynamic scaleProfile,
            ILogger logger
        ){

            onlineDeployment.properties.scaleSettings.minInstances = (int)scaleProfile["minInstances"];
            onlineDeployment.properties.scaleSettings.maxInstances = (int)scaleProfile["maxInstances"];
            onlineDeployment.properties.scaleSettings.instanceCount = (int)scaleProfile["instanceCount"];
            await OnlineEndpointsHelper.UpdateDeploymentResource(config, targetResourceId, onlineDeployment, logger);

        }

        static async Task<OnlineDeployment> GetOnlineDeployment(
            IConfigurationRoot config,
            string resourceID,
            ILogger logger
        ){

            return (await OnlineEndpointsHelper.GetDeploymentResource(config, resourceID, logger));

        }

        static dynamic GetScaleProfile(dynamic scheduleJson, ILogger logger){

            var profileName = scheduleJson["DefaultScaleProfile"].ToString();
            logger.LogInformation("Getting to here");

            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var dayOfWeek = DateTime.Now.DayOfWeek;

            var times = scheduleJson["Days"][dayOfWeek.ToString()];

            foreach (dynamic timeSlot in times){
                var startTime = TimeSpan.Parse(timeSlot["StartTime"].ToString());
                var endTime = TimeSpan.Parse(timeSlot["EndTime"].ToString());
                var currentTimeSpan = TimeSpan.Parse(currentTime);

                if (startTime <= endTime) {
                    // start and stop times are in the same day
                    if (currentTimeSpan >= startTime && currentTimeSpan <= endTime)
                    {
                        logger.LogInformation($"Current time fits into {timeSlot.Name}");
                        profileName = timeSlot["ScaleProfile"].ToString();
                    }
                } else {
                    // start and stop times are in different days
                    if (currentTimeSpan >= startTime || currentTimeSpan <= endTime) {
                    // current time is between start and stop
                        logger.LogInformation($"Current time fits into {timeSlot.Name}");
                        profileName = timeSlot["ScaleProfile"].ToString();
                    }
                }
            }

            return scheduleJson["ScaleProfiles"][profileName];

        }

        static dynamic ReadScheduleJson(string connectionString){

            // CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            // CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // CloudBlobContainer container = blobClient.GetContainerReference("scheduled-scaler");
            // CloudBlockBlob blockBlob = container.GetBlockBlobReference("schedule.json");

            // string text;
            // using (var memoryStream = new MemoryStream())
            // {
            //     blockBlob.DownloadToStream(memoryStream);
            //     text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            // }

            /* 
            For testing only!
            */
            var text = File.ReadAllText("schedule.json");
            return JsonConvert.DeserializeObject(text);

        }
    }
}
