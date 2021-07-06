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
            var profileName = scheduleJson.DefaultScaleProfile;
            var currentTime = DateTime.Now.ToString("HH:mm:ss");
            var dayOfWeek = DateTime.Now.DayOfWeek;
            ScaleProfile[] timeProfiles = null;

            foreach(var t in scheduleJson.Days){
                if (t.Name.Equals(dayOfWeek.ToString())){
                    timeProfiles = t.Profiles;
                }
            }

            foreach (var timeProfile in timeProfiles){
                var startTime = TimeSpan.Parse(timeProfile.StartTime);
                var endTime = TimeSpan.Parse(timeProfile.EndTime);
                var currentTimeSpan = TimeSpan.Parse(currentTime);

                if (startTime <= endTime) {
                    // start and stop times are in the same day
                    if (currentTimeSpan >= startTime && currentTimeSpan <= endTime)
                    {
                        logger.LogInformation($"Current time fits into {timeProfile.Name} time slot");
                        // profileName = timeSlot["ScaleProfile"].ToString();
                        profileName = timeProfile.DeploymentProfile;
                    }
                } else {
                    // start and stop times are in different days
                    if (currentTimeSpan >= startTime || currentTimeSpan <= endTime) {
                    // current time is between start and stop
                        logger.LogInformation($"Current time fits into {timeProfile.Name} time slot");
                        profileName = timeProfile.DeploymentProfile;
                    }
                }
            }

            bool profileExists = false;
            foreach(var profile in scheduleJson.DeploymentProfiles){
                if (profile.Name.Equals(profileName)){
                    profileExists = true;
                    return profile;
                }
            }

            if (!profileExists){
                throw new ArgumentException($"{profileName} does not exist as a DeploymentProfile in the schedule json file. Please confirm it exists.");
            }
            return null;
        }
    }
}