# IoT Event to SQL Server

**Produced by Dave Lusty**

## Introduction

This is a demo showing you how to use an Azure Function App to process incoming messages from IoT Hub and pass them to a SQL Server for further processing. This is useful in situations where your IoT devices might be sending in a JSON array rather than individual values. We can then use a Function App to separate these for individual processing. In this demo we will move them on to an Event Hub where other functions and apps can process them.

## Code

### local.settings.json
```JSON
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "Storage account connection string here",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "IoTHubConnection": "IoT Hub Connection String Here",
    "sqldbConnection": "SQL Server Connection String Here"
  }
}
```

### Function Code

At the top of the file we add in the various modules we need. This will include System.Threading.Tasks to allow us to process multiple events asynchronously, as well as Newtonsoft.Json.Linq to allow us to easily process the JSON data.

```CSHARP
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
```

Next is the actual function itself. You can include multiple functions in one code file if you wish, I kept them separate for simplicity in a demo.

```CSHARP

namespace IoTProcessor2
{
    public static class Function1
    {
        private static HttpClient client = new HttpClient();

```

Here we have puplic static async Task, indicating that this will spawn multiple tasks. Each task will write a single element from the incoming array to the SQL Server output. We're using tasks here to avoid latency on each event being processed, otherwise the function would need to wait for a response before processing the next element.
You can see the trigger in this line with the IoT Hub connection. I also added a Consumer Group (under "built in endpoints on the IoT Hub interface) for this function app. This consumer group ensures that this function has its own pointer within the data in the hub. If you create a second function, use a second consumer group to ensure both get all of the data. To scale you could also have two functions in the same consumer group, with each processing some of the data. 
Finally I added the execution context to allow us access to app settings such as the SQL connection string.
Note that since SQL Server is not a standard sink for Event Hubs that we do not have an output module in this function. Instead we use CSharp code to connect to the database and write out the data.
In all of these demos we write the incoming data out to a log so you can see what's happening. This is useful for troubleshooting but not necessary in a production envoironment.

```CSHARP

        [FunctionName("Function1")]
        public static async Task Run([IoTHubTrigger("messages/events", Connection = "IoTHubConnection", ConsumerGroup = "sqlfunction")]EventData message, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
```

Next we use some code to recall the app setting for SQL connection string. This is held by the Function App service for security reasons, and we recall it here to use for connecting to the database since we're not using an output module. This code snippet can be used to retrieve any variable you wish, just replace sqldbConnection in the last line with your setting name.

```CSHARP
            //get the SQL account info from application settings
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var sqlString = config["sqldbConnection"];
```

The first thing we do in the function is parse the JSON to an array so that we can process each element within it as a single message. Here we use the NewtonSoft JArray.Parse on the body of the message.

```CSHARP
            //split out the incoming JSON into an array
            JArray beaconRecords = JArray.Parse(Encoding.UTF8.GetString(message.Body.Array));
```
Finally we iterate through the array and insert the JSON into the database. Here I simply push the whole message into a field, but we could easily break out the data further and insert individual values.

```CSHARP
            using (SqlConnection conn = new SqlConnection(sqlString))
            {
                conn.Open();

                //Iterate through the array and submit to the eventhub
                foreach (var beaconRecord in beaconRecords.Children())
                {
                    var text = "INSERT INTO BeaconRecords (jsontext) VALUES ('" + beaconRecord.ToString() + "');";

                    using (SqlCommand cmd = new SqlCommand(text, conn))
                    {
                        // Execute the command and log the # rows affected.
                        var rows = await cmd.ExecuteNonQueryAsync();
                        log.LogInformation($"{rows} rows were updated");
                        //log the message being submitted, comment this out to improve performance
                        log.LogInformation("Added: " + beaconRecord);
                    }
                }
            }
        }
    }
}
```