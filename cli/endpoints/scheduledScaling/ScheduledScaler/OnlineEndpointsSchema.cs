namespace Microsoft.AzureML.OnlineEndpoints.RecipeFunction
{
    using System.Collections.Generic;

    public class ScaleSettings    {
        public string scaleType { get; set; } 
        public int instanceCount { get; set; } 
        public int minInstances { get; set; } 
        public int maxInstances { get; set; } 
    }

    public class ModelReference    {
        public string referenceType { get; set; } 
        public string id { get; set; } 
    }

    public class ContainerResourceRequirements    {
        public double cpu { get; set; } 
        public double memoryInGB { get; set; } 
    }

    public class DeploymentConfiguration    {
        public string computeType { get; set; } 
        public bool appInsightsEnabled { get; set; } 
        public ContainerResourceRequirements containerResourceRequirements { get; set; } 
        public string OSType { get; set; } 
        public string InstanceType { get; set; } 
        
    }

    public class OnlineDeploymentProperties    {
        public string description { get; set; } 
        public Dictionary<string, string> properties { get; set; } 
        public ScaleSettings scaleSettings { get; set; } 
        public ModelReference modelReference { get; set; } 
        public DeploymentConfiguration deploymentConfiguration { get; set; } 
        public string Type { get; set; }
    }

    public class Tags    {
        // public string key1 { get; set; } 
        // public string key2 { get; set; } 
    }

    public class OnlineDeployment    {
        public string location { get; set; } 
        public string kind { get; set; } 
        public OnlineDeploymentProperties properties { get; set; } 
        public Tags tags { get; set; } 
    }
}