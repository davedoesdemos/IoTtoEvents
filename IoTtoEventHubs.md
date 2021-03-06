# IoT Event to Event Hubs

**Produced by Dave Lusty**

## Introduction

This is a demo showing you how to use an Azure Function App to process incoming messages from IoT Hub and pass them to an Event Hub further processing. This is useful in situations where your IoT devices might be sending in a JSON array rather than individual values. We can then use a Function App to separate these for individual processing. In this demo we will move them on to an Event Hub where other functions and apps can process them.

## Code

### Create the Function App project

<table>
<tr>
<td width="60%">Open Visual Studio and select Create New Project.</td>
<td width="40%"><img src="images/newProject.png" /></td>
</tr>
<tr>
<td width="60%">Search for and select a new Azure Functions project using C#.</td>
<td width="40%"><img src="images/newFunctionApp.png" /></td>
</tr>
<tr>
<td width="60%">Give your project a name and location and click create.</td>
<td width="40%"><img src="images/newProjectName.png" /></td>
</tr>
<tr>
<td width="60%">Select IoT Hub as the trigger. We'll use the storage emulator for local testing, and give the app setting a name of IoTHubConnection.</td>
<td width="40%"><img src="images/trigger.png" /></td>
</tr>
</table>

### local.settings.json

In your project, open the local.settings.json file and add in values for the IoTHubConnection and eventHubConnection. Also fill in the AzureWebJobStorage with your storage account connection string which is used by the function app.

```JSON
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "Storage account connection string here",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "IoTHubConnection": "IoT Hub Connection String Here",
    "eventHubConnection": "Event Hub Connection String Here"
  }
}
```

Once you compile and upload your function you will need to add these settings to the Function App service in the portal.

### Function Code

At the top of the file we add in the various modules we need. This will include System.Threading.Tasks to allow us to process multiple events asynchronously, as well as Newtonsoft.Json.Linq to allow us to easily process the JSON data. You may also need to install the modules using NuGet.

```CSHARP
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
```

Next is the actual function itself. You can include multiple functions in one code file if you wish, I kept them separate for simplicity in a demo.

```CSHARP

namespace IoTProcessor
{
    public static class Function1
    {
        private static HttpClient client = new HttpClient();
```

Here we have puplic static async Task, indicating that this will spawn multiple tasks, you'll need to change this since the default is a standard function which returns one value and exits. Since we have one array coming in and then multiple messages going out we need to branch the code out. Each task will write a single element from the incoming array to the SQL Server output. We're using tasks here to avoid latency on each event being processed, otherwise the function would need to wait for a response before processing the next element.
You can see the trigger in this line with the IoT Hub connection. I also added a Consumer Group (under "built in endpoints on the IoT Hub interface) for this function app. This consumer group ensures that this function has its own pointer within the data in the hub. If you create a second function, use a second consumer group to ensure both get all of the data. To scale you could also have two functions in the same consumer group, with each processing some of the data. 
Finally we have the Event Hub output. This will be the sink for the function.
In all of these demos we write the incoming data out to a log so you can see what's happening. This is useful for troubleshooting but not necessary in a production envoironment.

```CSHARP

        [FunctionName("Function1")]
        public static async Task Run([IoTHubTrigger("messages/events", Connection = "IoTHubConnection", ConsumerGroup = "functionapp")]EventData message,
            [EventHub("dest", Connection = "eventHubConnection")] IAsyncCollector<string> outputEvents, 
            ILogger log)
        {
            //log the incoming body, comment this out to improve performance
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
```

The first thing we do in the function is parse the JSON to an array so that we can process each element within it as a single message. Here we use the NewtonSoft JArray.Parse on the body of the message.

```CSHARP
            //split out the incoming JSON into an array
            JArray beaconRecords = JArray.Parse(Encoding.UTF8.GetString(message.Body.Array));
```

Next a foreach loop can be used to iterate through the child elements one at a time and take an action. In this case we push the message onto the Event Hub by asynchronously adding a message to the output module. Azure Functions take care of all of the complexity here so we just fire the message then process the next. For a production use-case we would add in exception handlers to ensure that problems are detected. If this function fails then the IoT hub would simply resubmit the data to be processed again as it uses Service Bus internally with a pointer to the last processed data.

```CSHARP
            //Iterate through the array and submit to the eventhub
            foreach (var beaconRecord in beaconRecords.Children()) {
                await outputEvents.AddAsync(beaconRecord.ToString());
                //log the message being submitted, comment this out to improve performance
                log.LogInformation("Added: " + beaconRecord);
            }

        }
    }
}
```

Click to run the app locally and you will see data being processed in the log. First you'll see the incoming array and then each outgoing message. This will continue until all incoming data is processed. You can use Service Bus Explorer (a third party app) to see these messages on the Event Hub.

### Publish

<table>
<tr>
<td width="60%">Once you're ready to publish right click on the project and select publish.</td>
<td width="40%"><img src="images/publishApp.png" /></td>
</tr>
<tr>
<td width="60%">Select Azure as the target.</td>
<td width="40%"><img src="images/targetAzure.png" /></td>
</tr>
<tr>
<td width="60%">Select Function App Windows target.</td>
<td width="40%"><img src="images/targetWindows.png" /></td>
</tr>
<tr>
<td width="60%">You may need to log in, then select your Function App in your subscription. Click Finish to complete the task.</td>
<td width="40%"><img src="images/publishTarget.png" /></td>
</tr>
<tr>
<td width="60%">Click Deploy to push your code to Azure. You can later make changes and click this button to redeploy without needing to go through the whole wizard again.</td>
<td width="40%"><img src="images/Publish.png" /></td>
</tr>
<tr>
<td width="60%">In the portal, add the keys to match your local.settings.json file. Your app will then start to run and process data.</td>
<td width="40%"><img src="images/AddKeys.png" /></td>
</tr>
</table>