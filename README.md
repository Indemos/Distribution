# Distribution - Virtual Actor Framework

There are decent amount of actor-like frameworks on GitHub, including those that support an idea of Virtual Actors, e.g. `Orleans` and `Proto.Actor`  
The two mentioned frameworks are the closest to what I would like to use, but they have some drawbacks, like extra build step or inability to use different transport protocol. 
Below is the list of features built-in the current framework. 
Each of them can be considered as an advantage or a disadvantage depending on specific use-case. 

# Features

- Single thread 
- Zero configuration
- Cross platform .NET 6
- UDP broadcasting for automatic service discovery without a single point of failure 
- Organic Peer-To-Peer network of independent nodes without a single point of failure 
- Transparent placement of virtual actors within a cluster 
- Ability to create multiple instances of the same type of actor using unique ID 
- Ability to override any layer, property and method in the framework, including communication protocol, actor placement and activation strategy, peer discovery, etc
- No binary serialization or 3rd party serializtion libraries 
- Simple C# POCO classes as messages, no need in attributes or other decorators 
- Flexibility to process several messages within the same actor class or use different classes using `[Subscription]` attribute on top of the method 
- Usage of simplest Kestrel server middleware to process message queries 
- Usage of `Task` methods instead of FIFO loops for asynchronous communication 
- Automatic loading nad mapping for actors and messages using reflection 
- No use of locks

# Sample 

This is an example of using actors locally, within the same app. 

```C#
async Task SendMessageToLocalActor()
{
  var scene = new Scene();
  var message = new CreateMessage { Name = "Local Message" };
  var response = await scene.Send<DemoResponse>("Local Actor", message);

  Console.WriteLine("Local Response : " + response.Data);
}
```

An example with a distributed network of actors in the cluster is a bit more complex and can be found in the [Samples](https://github.com/Indemos/Distribution/tree/main/Samples) directory.

# Roadmap 

- PubSub streaming from cluster to client
- Performance improvement by replacing reflection with compiled delegates

# Disclaimer

In order to keep things simple and flexible, performance was sacrificed in favor of simplicity and scalability. 

**Drawbacks**

Practically all parts of this framework use the most basic implementation of each layer meaning that it's a general purpose implementation that may need to be extended to solve more specific problems. 

1. HTTP and JSON are somewhat slow, but were chosen for communication instead of sockets to make it easier to build a network of a million of nodes without a need to manage permanent connections between peers. 
2. UDP broadcasting can detect nodes within the same network segment. To make peer discovery global there will be a need for services like Consul or manual NAT traversing.
3. There is a heavy use of reflection for mapping between actors and messages. This module is not using compiled delegates yet.

**Improvements**

Even though existing modules are the most basic at the core, they can be easily extended or overridden to achieve most of specific goals. 

1. When there is no requirement to have millions of nodes, it's possible to implement `ICommunicator` and use sockets with Message Pack or Flat Buffers for much efficient performance. 
2. Peer service discovery is encapsulated inside of the `Beacon` class. When needed, it's easy to override any of its method or implement `IBeacon` to use it with `Consul` or some other tool. 
3. Switching from direct reflectino to compiled delegates for better performance is on the roadmap. 
