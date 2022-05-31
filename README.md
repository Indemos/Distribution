# Distribution - Virtual Actor Framework

There is decent amount of actor-like frameworks, including those that support an idea of Virtual Actors, e.g. `Orleans` and `Proto.Actor` 

The two mentioned frameworks are the closest to what I would like to use, but they have some drawbacks, like extra build step or inability to use different transport protocol. 
Below is the list of features built in the current framework. 
Each of them can be considered as an advantage or a disadvantage depending on specific use-case. 

# Features

- Single thread 
- Zero configuration
- Cross platform .NET 7
- UDP broadcasting for automatic service discovery without a single point of failure 
- Peer-To-Peer network of independent nodes without consensus leaders
- Random placement of virtual actors within a cluster 
- Ability to create multiple instances of the same type of actor using unique ID 
- Ability to override any layer, including protocol, actor placement strategy, service discovery, etc
- No dangerous binary serialization or 3rd party serializtion libraries 
- Simple C# POCO classes for messages, no attributes or other decorators 
- Process messages using the same or multiple actor classes with `[Subscription]` attribute 
- Kestrel server middleware to process message queries 
- Usage of `Task` methods instead of FIFO loops for asynchronous communication 
- Automatic loading and mapping for actors and messages using reflection, borrowed from `Mediatr` framework 
- No use of locks

# Nuget

```
Install-Package Distribution
```

# Sample 

This is an example of using actors locally within the same app. 

```C#
// Define message and response format 

public class DemoMessage { public string Name { get; set; }}
public class DemoResponse { public string Data { get; set; }}

// Define actor processing this particular message   

public class DemoActor
{
  [Processor]
  public virtual Task<DemoResponse> SomeAction(DemoMessage message)
  {
    return Task.FromResult(new DemoResponse { Data = "Response from actor" });
  }
}

// Processing

public class Client
{
  async Task SendMessageToActor()
  {
    var scene = new Scene();
    var message = new DemoMessage { Name = "Message to actor" };
    var response = await scene.Send<DemoResponse>("Custom Actor ID", message);

    Console.WriteLine("Response : " + response.Data);
  }
}
```

An example with a distributed network of actors in the cluster is a bit more complex and can be found in the [Samples](https://github.com/Indemos/Distribution/tree/main/Samples) directory.

# Disclaimer

In order to keep things simple and flexible, the main focus was on simplicity and scalability rather than performance. 

**Notes**

Practically all parts of this framework use the most basic implementation of each layer meaning that it's a general purpose implementation that may need to be extended to solve more specific problems. 

1. HTTP and JSON are somewhat slow, but were chosen for communication instead of sockets to make it easier to build a network of a million of nodes without a need to manage permanent connections between peers. 
2. UDP broadcasting can detect nodes within the same network segment. To make peer discovery global there will be a need for services like Consul or manual NAT traversing.
3. There is a heavy use of reflection for mapping between actors and messages. No benchmarks, but switching to compiled delegates may improve latency.

**Improvements**

Even though existing modules are the most basic at the core, they can be easily extended or overridden to achieve more specific goals. 

1. When there is no requirement to have millions of nodes, it's possible to implement `ICommunicator` and use sockets with Message Pack or Flat Buffers for lower latency. 
2. Peer discovery is encapsulated inside of the `Beacon` class. When needed, it's easy to override any of its method or implement `IBeacon` to use it with `Consul` or some other service discovery tool. 
