# iotEdgePerf
A framework and a CLI tool to measure throughput and end-to-end latency of an IoT Edge inspired by this blog post: https://aka.ms/IotEdgePerf 

Useful for:
* measuring the rate/throughput at transmitter egress (A) and iot hub ingress (B) along with the A-to-B latency
* sizing HW (or VM) to meet the target rate/latency or assessing the maximum rate/throughput achievable on a given configuration
* optimizing rates/latency by fine-tuning message batching

![](./images/architecture.png)

The framework includes:

* a [transmitter module](source/IotEdgePerf.Transmitter.Edge/) (1), to generate traffic 
  (arlotito/iotedgeperf-transmitter [![for easy](https://img.shields.io/docker/v/arlotito/iotedgeperf-transmitter)](https://hub.docker.com/repository/docker/arlotito/iotedgeperf-transmitter))
* an [IotEdgePer.Profiler](source/IotEdgePerf.Profiler/) class (2), used by the transmitter module. It can also be used to instrument and profile your code
* an [ASA query](./asa/) (3), to measure the ingestion latency and rate
* the [IotEdgePer.ConsoleApp](source/IotEdgePerf.ConsoleApp/) (4) CLI app, to control the transmitter, to analyze the data produced by the ASA job and show the results

An example:
```bash
dotnet run -- --payload-length=2048 --burst-length=10000 target-rate=500
```
![](/images/simple-example.png)

# Getting started
Pre-requisites:
* a TEST device (VM or real HW) provisioned with IoT Edge 1.1/1.2
* a linux DEV machine 
* Azure IoT Hub, Azure Stream Analytics job (see above), Azure Event Hubs
* optional: log analytics workspace for logging edge metrics to the cloud

## Prep the IoT Edge
Log-in into the iot edge device and create the '/iotedge' folder (will be used to bind the edgeHub's folder): 
```bash
sudo mkdir -p /iotedge && sudo chown -R 1000 /iotedge && sudo chmod 700 /iotedge
```
At any time you can check the size consumed by the edgeHub queue with:
```bash
du -hd1 /iotedge
```

## Execute the tests

The performance tests can be done with the .NET Console app, which requires .NET 5.
The app has two modes:
- `deploy` for deploying Azure IoT Edge modules on the pre-provisioned edge.
- `runtest` mode for running multiple iterations of the tests.

### Deploy the transmitter module (without metrics-collector) to the pre-provisioned Edge Device Under Test (DUT)
Deploying the modules to your IoT Edge only needs to be done once, afterwards you can run the performance test multiple times without needing to redeploy the Edge modules.

```bash
dotnet run -p ./source/IotEdgePerf.ConsoleApp -- deploy  --device-id="myedgedeviceid" --image-uri "arlotito/iotedgeperf-transmitter:0.6.0" -b 200
```

The parameter "-b 200" sets ["MaxUpstreamBatchSize"](https://github.com/Azure/iotedge/blob/master/doc/EnvironmentVariables.md) to 200. 
Change it to fit your needs.

### Deploy the transmitter module with metrics-collector to the DUT
In order to deploy the required modules, and additionally add the [Metrics Collector](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-collect-and-transport-metrics?view=iotedge-2020-11&tabs=iothub#metrics-collector-module) module, you need to gather the following settings to pass on to the app:
- `log-a-workspaceid`: This is the Log Analytics Workspace ID. It's in the form of a GUID.
- `log-a-iotresourceid`: This is the IoT Hub's Resource ID.
- `log-a-key`: the log analytics shared key.

```bash
dotnet run -p ./source/IotEdgePerf.ConsoleApp -- deploy \
 --device-id="myedgedeviceid" \
--image-uri "arlotito/iotedgeperf-transmitter:0.6.0" \
--log-a-workspaceid "/subscriptions/xyz/resourceGroups/xyz/providers/Microsoft.Devices/IotHubs/xyz" \
--log-a-iotresourceid "xyzguid" \
--log-a-key "xyzkey"
                                   
```

### Run the Console APP from any machine 

```bash
dotnet run -p ./source/IotEdgePerf.ConsoleApp -- runtest \
  --ehName="myEventHubName" \
  --eh-conn-string="Endpoint=sb://xyz.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxx" \
  --iot-conn-string="HostName=xxx;SharedAccessKeyName=service;SharedAccessKey=xxx" \
  --device-id="myEdgeDevice" \
  --payload-length=2048 \
  --burst-length=10000 \
  --target-rate=500 \
  -o test.csv
```

And here's the result:
![screnshot of the CLI output](./images/simple-example.png)

To make the command line command shorter, some parameters can laso be passed over via ENV variables:
```bash
export DEVICE_ID=__redacted__
export EH_NAME=__redacted__
export EH_CONN_STRING=__redacted__
export IOT_CONN_STRING=__redacted__
export IOT_HUB_NAME=__redacted__
```
and then run with:
```bash
dotnet run -p ./source/IotEdgePerf.ConsoleApp -- runtest \
  --device-id="myEdgeDevice" \
  --payload-length=1024 \
  --burst-length=10000 \
  --target-rate=2000 \
  -o test.csv
```

## Optionally build the iotEdgePerf tool
On the DEV machine, build the iotEdgePerfTool as a self-contained binary:
```bash
dotnet publish ./source/iotEdgePerf/iotEdgePerf.csproj -r linux-x64 -p:PublishSingleFile=true --configuration Release -o .
```





