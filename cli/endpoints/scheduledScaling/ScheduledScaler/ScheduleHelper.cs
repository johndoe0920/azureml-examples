using System;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    /// <summary>
    /// Online endpoint client to get and update deployments.
    /// </summary>
    public class ScheduleHelper
    {

        public static ScaleSchedule ReadScheduleJson(
            string connectionString,
            string containerName, 
            string scheduleFileName
        ){
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(scheduleFileName);

            string text;
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            return JsonConvert.DeserializeObject<ScaleSchedule>(text);

            /* 
            For testing only!
            */
            // var text = File.ReadAllText(scheduleFileName);
            // return JsonConvert.DeserializeObject<ScaleSchedule>(text);

        }
        
        public static DeploymentProfile GetScaleProfile(
            ScaleSchedule scheduleJson, 
            ILogger logger
        ){
            logger.LogInformation($"Retrieving Default Scale Profile");
            string depProfileName = scheduleJson.DefaultScaleProfile;
            if(String.IsNullOrEmpty(depProfileName)){
                logger.LogWarning("DefaultScaleProfile is not set in schedule JSON file. Please consider designating a default scale profile!");
            }

            var currentTimeSlot = GetCurrentTimeSlot(scheduleJson, logger);

            logger.LogInformation($"Current time slot to use is {currentTimeSlot}");
            
            depProfileName = GetDeploymentProfileName(scheduleJson, currentTimeSlot, depProfileName);

            bool profileExists = false;
            foreach(var depProfile in scheduleJson.DeploymentProfiles){
                if (depProfile.Name.Equals(depProfileName)){
                    profileExists = true;
                    return depProfile;
                }
            }

            if (!profileExists){
                throw new ArgumentException($"Deployment scale profile {depProfileName} does not exist as a DeploymentProfile in the schedule json file. Please confirm it exists.");
            }
            return null;
        }

        public static string GetCurrentTimeSlot(
            ScaleSchedule scheduleJson,
            ILogger logger
        ){
            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var dayOfWeek = DateTime.Now.DayOfWeek;

            string currentTimeSlot = "";
            var currentTimeSpan = TimeSpan.Parse(currentTime);

            foreach(var ts in scheduleJson.TimeSlots){
                var startTime = TimeSpan.Parse(ts.StartTime);
                var endTime = TimeSpan.Parse(ts.EndTime);

                if (startTime <= endTime) {
                    // start and stop times are in the same day
                    if (currentTimeSpan >= startTime && currentTimeSpan <= endTime)
                    {
                        logger.LogInformation($"Current time fits into {ts.Name} time slot");
                        currentTimeSlot = ts.Name;
                    }
                } else {
                    // start and stop times are in different days
                    if (currentTimeSpan >= startTime || currentTimeSpan <= endTime) {
                    // current time is between start and stop
                        logger.LogInformation($"Current time fits into {ts.Name} time slot");
                        currentTimeSlot = ts.Name;
                    }
                }
            }

            return currentTimeSlot;
        }

        public static string GetDeploymentProfileName(
            ScaleSchedule scheduleJson,
            string currentTimeSlot,
            string depProfileName
        ){
            var dayOfWeek = DateTime.Now.DayOfWeek;
            foreach(var day in scheduleJson.Days){
                if (day.Name.Equals(dayOfWeek.ToString())){
                    foreach(var profile in day.Profiles){
                        if (profile.TimeSlot.Equals(currentTimeSlot)){
                            depProfileName = profile.DeploymentProfile;
                        }
                    }
                }
            }
            return depProfileName;
        }
    }
}