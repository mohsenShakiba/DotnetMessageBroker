# Message Broker 
This repo contains an implementation of a message broker server and client written in C# and .NET 5.
Its structure and architecture is very simple and extendable ideal for trying new ideas.

## Why a .NET message broker?
I wanted to create a C# implementation of message broker to improve my skills in 
socket programming and binary programming. It was merely a challenge for me to learn new 
things.

## Is it production ready?
It is and I'm currently using it in production but maybe you are better off using other 
more popular and battle tested solutions.

## Setting up server
To setup a server you have two options:
- Create server from scratch
- Use Docker image

### Create server from scratch
Creating server is very straight forward, first add the package

```
dotnet add package MessageBroker.Core
```
Then create server using `BrokerBuilder`:

```c#
using var broker = new BrokerBuilder()
    .UseEndPoint(IPEndPoint.Parse("0.0.0.0:8080"))
    .UseMemoryStore()
    .Build();
```

you can further configure the server for example to configure the logging configuration:

```c#
...
.ConfigureLogger(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
})
```

### Use Docker image
There is a dedicated Docker image that will create a message broker server:

```
docker pull mshakiba/messagebroker
docker run -d -p 8080:8080 mshakiba/messagebroker
```

Additional configuration can be provided such as the port and log level using volumes:
```
docker run -d -p 8080:8080 -v ./appsettings.json:/app/appsettings.json mshakiba/messagebroker
```

A simple configuration file:
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information"
        }
    },
    "EndPoint": "0.0.0.0:8080"
}
```

## Using Client
To connect the server you need the client:
```
dotnet add package MessageBroker.Client
```

### Configuring client
```c#
// creating a new broker client
await using var brokerClient = new BrokerClientFactory().GetClient()
    
// connecting the client to server
brokerClient.Connect(new ClientConnectionConfiguration
{
    EndPoint = new DnsEndPoint("localhost", 8080)
})
```

### Creating And Delete Topic
To start sending message we need to first define a topic
```c#
// creating topic
var createResponse = await brokerClient.DeclareTopicAsync("MyTopic", "/my/topic");
Assert.True(createResponse.IsSuccess)

// deleting topic
var deleteResponse = await brokerClient.DeleteTopicAsync("MyTopic");
Assert.True(deleteResponse.IsSuccess)
```
Name of topic is just a string that is used for identifying topics, the route on the 
other hand is used for routing.

### Creating subscription
A subscription is used for receiving messages sent to a topic, for example all the 
messages sent to `MyTopic` can be received by creating a subscription to the topic:
```c#
var subscription = await brokerClient.GetTopicSubscriptionAsync("MyTopic");

subscription.MessageReceived += (msg) => {

    // get the message data
    var data = msg.Data;
    
    // ack the message
    msg.Ack();
    
    // or optionally nack
    // msg.Nack()
}
```
Note: when receiving message make sure to either call `Ack` or `Nack` otherwise
the server will think that you are still processing the message and will continue to 
think so until the subscription is disposed.

### Publishing message
To publish message we need to serialize our payload in binary format and provide the 
route we want the message to be delivered to:
```c#
var response = await brokerClient.PublishAsync(binaryData, "/my/topic");
Assert.True(response.IsSuccess);
```

## Routing
The routing in message broker follows these simple rules:
- If two routes are identical then they will match
- If one of the routes are empty or is a wildcard then they will match
- If route of topic is a subset of the message route then they will match

For example:
- `foo/bar` and `foo/bar` will match
- `foo` and `foo/bar` will match
- `foo/*` and `foo/bar` will match  
- `*` and `foo/bar` will match
- `foo/bar/foo` and `foo/bar` will **NOT** match
Note: left side is the topic route and the right side is the message route