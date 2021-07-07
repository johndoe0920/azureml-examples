namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    public class ScaleSchedule{

        public string DefaultScaleProfile {get;set;}
        public DaySettings[] Days {get;set;}
        public DeploymentProfile[] DeploymentProfiles {get;set;}

        public TimeSlot[] TimeSlots {get;set;}

    }

    public class DaySettings{
        public string Name {get;set;}
        public ScaleProfile[] Profiles {get;set;}
    }

    public class ScaleProfile{
        public string TimeSlot {get;set;}
        public string DeploymentProfile {get;set;}
    }

    public class TimeSlot{
        public string Name {get;set;}
        public string StartTime {get;set;}
        public string EndTime {get;set;}
    }

    public class DeploymentProfile{
        public string Name {get;set;}
        public int instanceCount {get;set;}
        public int minInstances {get;set;}
        public int maxInstances {get;set;}
        public int ScaleOutStep {get;set;}
        public int ScaleInStep {get;set;}
    }

}