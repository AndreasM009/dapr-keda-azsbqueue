# Producer Consumer pattern with scalable consumer using dapr, keda and Azure ServiceBus Queues

I think every architect or developer knows about the producer consumer pattern. In this repository I want to show how [dapr bindings](http://github.com/dapr) can be used to implement this pattern and how the consumer can be scaled out with [keda](https://github.com/kedacore/keda).

// Todo: create a architecture diagram

In dapr you can use output and input bindings to send message to and receive messages from a queue. 
When you decide on a queue technique like Redis, RabbitMQ or Azure ServiceBus Queues, you usually have to use integration libraries for binding in your code.
With dapr you can integrate input and output bindings on a higher abstraction level and you don't need to know how the integration library works. 
In this example an Azure ServiceBus Queue is used to send and receive messages. The Producer and Consumer is implemented already in Asp.NET Core 3.1. The application code is avalaible as a docker image on my Docker Hub repository:
- Producer: m009/producer:0.1
- Consumer: m009/consumer:0.1

## Dapr Component
Dapr bindings are described in Kubernetes as Custom Resource Definitions of kind __Component__.
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: message-queue
spec:
  type: bindings.azure.servicebusqueues
  metadata:
    - name: connectionString
      secretKeyRef:
        name: servicebus-management
        key: servicebus-management-connectionstring
    - name: queueName
      value: "msgqueue"
```  

In the __spec__ section we specify the type of the binding __bindings.azure.servicebusqueue__.
In addition, the connection string (Manage namespace) of the Azure ServiceBus instance and the name of the Queue must be specified.

The following secret is used to store the connection string.
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: servicebus-management
  labels:
    app: bindingconsumer
data:
  servicebus-management-connectionstring: "<your base64 encoded connection string>"
type: Opaque
```

// Todo