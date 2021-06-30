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

        public static dynamic ReadScheduleJson(
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

            return JsonConvert.DeserializeObject(text);

            /* 
            For testing only!
            */

            // var text = File.ReadAllText(scheduleFileName);
            // return JsonConvert.DeserializeObject(text);

        }
        
        public static dynamic GetScaleProfile(
            dynamic scheduleJson, 
            ILogger logger
        ){

            var profileName = scheduleJson["DefaultScaleProfile"].ToString();
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
                        logger.LogInformation($"Current time fits into {timeSlot.Name} time slot.");
                        profileName = timeSlot["ScaleProfile"].ToString();
                    }
                } else {
                    // start and stop times are in different days
                    if (currentTimeSpan >= startTime || currentTimeSpan <= endTime) {
                    // current time is between start and stop
                        logger.LogInformation($"Current time fits into {timeSlot.Name} time slot.");
                        profileName = timeSlot["ScaleProfile"].ToString();
                    }
                }
            }
            return scheduleJson["ScaleProfiles"][profileName];
        }
    }
}