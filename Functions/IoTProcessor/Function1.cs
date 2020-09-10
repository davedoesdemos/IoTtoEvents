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

namespace IoTProcessor
{
    public static class Function1
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("Function1")]
        public static async Task Run([IoTHubTrigger("messages/events", Connection = "IoTHubConnection", ConsumerGroup = "functionapp")]EventData message,
            [EventHub("dest", Connection = "eventHubConnection")] IAsyncCollector<string> outputEvents, 
            ILogger log)
        {
            //log the incoming body, comment this out to improve performance
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            //split out the incoming JSON into an array
            JArray beaconRecords = JArray.Parse(Encoding.UTF8.GetString(message.Body.Array));

            //Iterate through the array and submit to the eventhub
            foreach (var beaconRecord in beaconRecords.Children()) {
                await outputEvents.AddAsync(beaconRecord.ToString());
                //log the message being submitted, comment this out to improve performance
                log.LogInformation("Added: " + beaconRecord);
            }

        }
    }
}