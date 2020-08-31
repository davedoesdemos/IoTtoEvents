# IoT Event Processor

**Produced by Dave Lusty**

## Introduction

This is a demo showing you how to use an Azure Function App to process incoming messages from IoT Hub and pass them to an Event Hub for further processing. This is useful in situations where your IoT devices might be sending in a JSON array rather than individual values. We can then use a Function App to separate these for individual processing. In this demo we will move them on to an Event Hub where other functions and apps can process them.

There is a [video of this demo not ready yet](https://youtube.com/davedoesdemos)

## Trigger

For this project we will be using the IoT Hub trigger to process messages from an IoT Hub. Documentation for the IoT Hub trigger is available at https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-iot-trigger?tabs=csharp
This trigger is the same as the Event Hubs trigger, and so code will work the same for both.

Of particular note is the ConsumerGroup property which allows us to specify a consumer group for reading the messages. This, in turn, allows us to process messages in more than one application by creating consumer groups for each. This ensures that each application has a checkpoint and processes all messages.

```csharp
  public static void Run([IoTHubTrigger("messages/events", Connection = "IoTHubConnection", ConsumerGroup = "functionapp")]EventData message, ILogger log)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
        }
```

## Parsing the JSON

Using Newtonsoft JSON.net https://www.newtonsoft.com/json/help/html/QueryJsonDynamic.htm

```csharp
string json = @"[
  {
    'Title': 'Json.NET is awesome!',
    'Author': {
      'Name': 'James Newton-King',
      'Twitter': '@JamesNK',
      'Picture': '/jamesnk.png'
    },
    'Date': '2013-01-23T19:30:00',
    'BodyHtml': '&lt;h3&gt;Title!&lt;/h3&gt;\r\n&lt;p&gt;Content!&lt;/p&gt;'
  }
]";

dynamic blogPosts = JArray.Parse(json);

dynamic blogPost = blogPosts[0];

string title = blogPost.Title;

Console.WriteLine(title);
// Json.NET is awesome!

string author = blogPost.Author.Name;

Console.WriteLine(author);
// James Newton-King

DateTime postDate = blogPost.Date;

Console.WriteLine(postDate);
// 23/01/2013 7:30:00 p.m.
```

https://www.newtonsoft.com/json/help/html/SerializeObject.htm

```csharp
Account account = new Account
{
    Email = "james@example.com",
    Active = true,
    CreatedDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc),
    Roles = new List<string>
    {
        "User",
        "Admin"
    }
};

string json = JsonConvert.SerializeObject(account, Formatting.Indented);
// {
//   "Email": "james@example.com",
//   "Active": true,
//   "CreatedDate": "2013-01-20T00:00:00Z",
//   "Roles": [
//     "User",
//     "Admin"
//   ]
// }

Console.WriteLine(json);
```