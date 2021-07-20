# Introduction

This project is a solution to address scheduled scaling for online endpoints and deployments. Customers have asked for a way to set a time table of when they want their deployments to scale up and down to meet the expected traffic that will hit their models following predictable patterns (low traffic weekends, high traffic during work hours, etc.).

This solution uses a simple C# project that runs in Azure Functions. It's intended to be deployed via an ARM deployment using the template files in the `deployment` folder and sets up all the necessary resources to set up scheduling for your deployment.

## Prerequisities

In order to properly use this solution, it is expected:

1. You have an existing workspace, endpoint and deployment created.
1. You have permissions to add a user create system identity (MSI) to the workspace and permissions to create resources inside the subscription.
1. A version of the `az cli` that can perform an ARM template deployment.

## Define Parameters For Deployment

In `deployment/template.json`, you can see the parameters section that declares a series of variables that will be used for the deployment. You will need to define these values in the parameters.json file using information like your subscription ID, workspace name, deployment name, etc.

For example:

```json
"parameters": {
    "BaseName": {
        "value": "ScheduledScaler"
    },
    "IdentityName": {                   //  Name of the MSI that will be created. In this example, rowagne-msi.
        "value": "rowagne-msi"
    },
    "WorkspaceResourceGroupName": {     //  Name of the resource group where your workspace lives. In this example, rowagne-test.
        "value": "rowagne-test"
    }
}
```

## Setup Your Schedule File

For this functionality, you'll use a basic JSON file to define time slots, deployment profile and then assign time spans for each day of the week with which deployment profile to use.

In the below example, you can see the settings for Monday where there are two time slots set, Morning and Afternoon, with what Deployment Profile to use.

In the Timeslots section you can define what hours (in UTC) correspond to these Time Slots.

In the Deployment Profiles, you can define different settings for the deployment to be changed (instance count, max and min instances, etc.)

```json
{
    "DefaultScaleProfile": "LowUse",
    "Days": [
        {
            "Name": "Monday",
            "Profiles": [
                {
                    "TimeSlot": "Morning",
                    "DeploymentProfile": "LowUse"
                },
                {
                    "TimeSlot": "Afternoon",
                    "DeploymentProfile": "HighUse" 
                }
            ]
        }
    ],
    "TimeSlots": [
        {
            "Name": "Morning",
            "StartTime": "00:00:00",
            "EndTime": "12:00:00"
        },
        {
            "Name": "Afternoon",
            "StartTime": "12:00:00",
            "EndTime": "18:00:00"
        }
    ],
    "DeploymentProfiles": [
        {
            "Name": "LowUse",
            "instanceCount": 1,
            "minInstances": 2,
            "maxInstances": 3,
            "ScaleOutStep": 1,
            "ScaleInStep": 3
        },
        {
            "Name": "HighUse",
            "instanceCount": 2,
            "minInstances": 2,
            "maxInstances": 3,
            "ScaleOutStep": 1,
            "ScaleInStep": 3
        }
    ]
```

A simple `ScheduleScaler/schedule.json` file has been provided as a starting point. Please make changes as you see fit to best fit your expected traffic patterns.

### Note: Please remember that right now times should be based on UTC. Timezone setting is a planned feature but has not been implemented yet.

## Deploy via ARM

In a terminal with `az cli` in the `deployment` directory, you can run 

`az group deployment create -n <Function_name> -g <resource_group_name> --template-file "template.json" --parameters "parameters.json"`

What this does is will create all the resources that are listed in the template file which includes the Function App, the service plan, a storage account, a user MSI, and an Application Insights instance.

### Note: If you want, there is a way to use names of existing resources instead of creating new resources.

Once the resources are created, a function inside the Function App will be created by doing a git clone of this repo (and the branch that is defined) and starting the code located in the `ScheduledScaler` directory. The function at this point will fail for two reasons: 1) The schedule.json file needs to be uploaded to the storage blob and has to be done so manually and 2) The new user-msi needs permissions on the workspace in order to read info on your existing deployment and make changes to it.

## Final Manual Steps

First you need to upload your schedule json file into the blob container that was created in your storage account created during the ARM deployment. The name of the container and the schedule file are both configurable, so please upload a file with the name of `ScheduleFileName` into `ContainerName` defined in the `parameters.json` file.

### Note: At any time, you can change this schedule file since it gets downloaded every time the function runs. You can also change the name of the container and schedule file in the Configuration settings of the Function App.

Second, you need to assign permissions to the newly made MSI named `IdentityName` to your workspace that your deployment belongs to. The role you assign it must have read permissions on your workspace, deployments and endpoints, and write permissions on your deployments. Something like Contributor covers these needs, but may be too powerful of an assignment to give to an MSI for some teams.

## Conclusion

With the schedule file uploaded and the user MSI given the correct permissions, your function should start to operate successfully. You can go to the function itself to view the logs and see if it has succeeded by going to the `Functions` tab in the Function App. The function will execute once every minute and logs usually take about five minutes to show up in the Azure Portal (due to App Insights delay).

## Further Automation

Currently, this doc is designed to explain how this function works and what's needed to get it working with an existing deployment, workspace, endpoint, etc. However, it should be possible to merge this ARM template and parameters into an existing ARM template used to deploy a workspace, endpoint and deployment to make it a more complete automated system. Since this template tells the Function App to download the source code for the function, the template json is completely transplantable into any ARM deployment template.
