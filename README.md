# Producer Consumer pattern with scalable consumer using dapr, keda and Azure ServiceBus Queues

I think every architect or developer knows about the producer consumer pattern. In this repository I want to show how [dapr bindings](http://github.com/dapr) can be used to implement this pattern and how the consumer can be scaled out with [keda](https://github.com/kedacore/keda).

// Todo: create a architecture diagram

In dapr you can use output and input bindings to send message to and receive messages from a queue. 
When you decide on a queue technique like Redis, RabbitMQ or Azure ServiceBus Queues, you usually have to use integration libraries for binding in your code.
With dapr you can integrate input and output bindings on a higher abstraction level and you don't need to know how the integration library works. 

In this sample we create two microservices, one with an input binding, the __Consumer__, and one with an output binding, the __Producer__. We will bind to Azure ServiceBus Queues and scale out the __Consumer__ on demand depending on how many messages are in the instance of a Azure ServiceBus Queue. We will use [keda](https://github.com/kedacore/keda) to provide scaling metrics for the Horizontal Pod Austoscaler. 

The __Producer__ and __Consumer__ are implemented already in Asp.NET Core 3.1. The application code is avalaible as a docker image on my Docker Hub repository:
- Producer: m009/producer:0.1
- Consumer: m009/consumer:0.1

## Prerequisites
- [Dapr enabled Kubernetes Cluster](https://github.com/dapr/docs/blob/master/getting-started/environment-setup.md#installing-dapr-on-a-kubernetes-cluster)
- [Keda enabled Kubernetes Cluster](https://keda.sh/deploy/)

## Run the application

### Setting up Azure ServiceBus Queue
1. Create a new or use an existing Azure ResourceGroup
```Shell
az group create -n <your RG name> -l <location>
```
2. Create a new ServiceBus Namespace
```
az servicebus namespace create -n <namespace name> -g <your RG name> -l <location> --sku Basic
```
3. We need to be able to manage the namespace, therefore we need to list the __RootManageSharedAccessKey__ connection string
```Shell
az servicebus namespace authorization-rule keys list -g <your RG name> --namespace-name <namespace name> --name RootManageSharedAccessKey
```
The output looks as follow:
```JSON
{
  "aliasPrimaryConnectionString": null,
  "aliasSecondaryConnectionString": null,
  "keyName": "RootManageSharedAccessKey",
  "primaryConnectionString": "<connstr1>",
  "primaryKey": "<redacted>",
  "secondaryConnectionString": "<connstr2>",
  "secondaryKey": "<redacted>"
}
```
Create a base64 representation of the connection string (either use primary or secondary)
```Shell
echo -n '<connstr1>' | base64
```
Update the Kubernetes secret in [binding-deployment.yaml](deploy/binding-deployment.yaml) with the base64 encoded value.
4. Create a ServiceBus Queue
```
az servicebus queue create -n msgqueue -g <your RG name> --namespace-name <namespace name>
```
5. We need to be able to connect to the queue, therefore we need to create an auth rule with __Manage__ permission
```Shell
az servicebus queue authorization-rule create -g <your RG name> --namespace-name <namespace name> --queue-name msgqueue --name manage --rights Manage
``` 
Once the auth rule is created we can list the connection string as follow:
```
az servicebus queue authorization-rule keys list -g <your RG name> --namespace-name <namespace-name> --queue msgqueue 
```
The output looks as follow:
```JSON
{
  "aliasPrimaryConnectionString": null,
  "aliasSecondaryConnectionString": null,
  "keyName": "order-consumer",
  "primaryConnectionString": "<connstr1>",
  "primaryKey": "<redacted>",
  "secondaryConnectionString": "<connstr2>",
  "secondaryKey": "<redacted>"
}
```
Create a base64 representation of the connection string (either use primary or secondary)
```Shell
echo -n '<connstr1>' | base64
```
Update the Kubernetes secret in [consumer-scaling.yaml](deploy/consumer-scaling.yaml) with the base64 encoded value.

## How it works

### Dapr Component
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