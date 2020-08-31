using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace cSharpSimulator
{
    class simulatorBeacon
    {
        class recordDatapoint
        {
            public int S;
            public int e;
            public int f;
            public int o;
            public int a;
            public int r;
            public int p;
        }
        class beaconRecord
        {
            public int m;
            public int t;
            public List<recordDatapoint> d;
        }

        private static DeviceClient s_deviceClient;

        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync()
        {
            //keep sending data until we quit the program
            while (true)
            {
                //initialise random seed
                Random rand = new Random();

                //how many records to return
                int numberOfRecords = rand.Next(1, 30);

                //create the list to send
                List<beaconRecord> beaconRecords = new List<beaconRecord>();

                //iterate to create the records in the list
                for (int i = 0; i < numberOfRecords; i++)
                {
                    beaconRecord newBeaconRecord = new beaconRecord();
                    newBeaconRecord.d = new List<recordDatapoint>();
                    newBeaconRecord.m = rand.Next(0, 1000000);
                    newBeaconRecord.t = rand.Next(1, 1000);

                    //how many data points for this record
                    int numberOfDatapoints = rand.Next(1, 30);

                    //iterate to create the datapoints in the list of the record
                    for (int j = 0; j < numberOfDatapoints; j++)
                    {
                        //create a new data point and fill it with data
                        recordDatapoint newDatapoint = new recordDatapoint();
                        newDatapoint.S = rand.Next(0, 250);
                        newDatapoint.e = rand.Next(0, 50);
                        newDatapoint.f = rand.Next(0, 20);
                        newDatapoint.o = rand.Next(0, 24450);
                        newDatapoint.a = rand.Next(0, 2502);
                        newDatapoint.r = rand.Next(0, 20);
                        newDatapoint.p = rand.Next(0, 2000000);

                        //add this datapoint to the list in the parent object
                        newBeaconRecord.d.Add(newDatapoint);
                    }

                    //add this record to the list for sending
                    beaconRecords.Add(newBeaconRecord);
                }

                var messageString = JsonConvert.SerializeObject(beaconRecords);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Send the telemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Beacon Simulator. Ctrl-C to exit.\n");
            if (args.Length == 0)
            {
                Console.WriteLine("Please add your IoT Hub connection string as an argument at the command line");
            } else
            {
                String s_connectionString = args[0];
                Console.WriteLine("Connection string is: " + args[0]);
                // Connect to the IoT hub using the MQTT protocol
                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
                SendDeviceToCloudMessagesAsync();
                Console.ReadLine();
            }

        }
    }
}
