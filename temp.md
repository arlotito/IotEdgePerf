Optionally, the metrics collector (3), to collect the metrics (edgehub and edgeagent built-in, source and sink modules) and send them to Azure Monitor
* a [Jupyter notebook](./jupyter/) (6), as an alternative to the console application, to collect and visualize relevant metrics


Here's a simple test configuration and the related [deployment.test1.json](deployment.test1.json):
![](./images/simple-example.png)

Here's the relevant section of [deployment.test1.json](deployment.test1.json), where you can adjust:
* the source stream parameters (rate, num of messages, ...) (full details in the [source module](./edgeSolution/modules/source) docs)
* your log analytics workspace info in place of `<YOURSHERE>`

```json
      "modules": {
          "source": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "arlotito/edge-sim-source:0.3.9-amd64",
              "createOptions": "{}"
            },
            "env": {
              "START_WAIT": {
                "value": "10000"
              },
              "BURST_LENGTH": {
                "value": "1500"
              },
              "BURST_WAIT": {
                "value": "10000"
              },
              "BURST_NUMBER": {
                "value": "1"
              },
              "TARGET_RATE": {
                "value": "50"
              },
              "LOG_MSG": {
                "value": "false"
              },
              "LOG_BURST": {
                "value": "true"
              },
              "LOG_HIST": {
                "value": "false"
              },
              "MESSAGE_PAYLOAD_LENGTH": {
                "value": "1024"
              },
              "RATE_CALC_PERIOD": {
                "value": "5000"
              }
            }
          },
          "IoTEdgeMetricsCollector": {
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-metrics-collector:1.0",
              "createOptions": ""
            },
            "type": "docker",
            "env": {
              "ResourceId": {
                "value": "/subscriptions/<YOURSHERE>/resourceGroups/edge-benchmark-hub-rg/providers/Microsoft.Devices/IotHubs/<YOURSHERE>"
              },
              "UploadTarget": {
                "value": "AzureMonitor"
              },
              "LogAnalyticsWorkspaceId": {
                "value": "<YOURSHERE>"
              },
              "LogAnalyticsSharedKey": {
                "value": "<YOURSHERE>"
              },
              "ScrapeFrequencyInSecs": {
                "value": "2"
              },
              "MetricsEndpointsCSV": {
                "value": "http://edgeAgent:9600/metrics, http://edgeHub:9600/metrics, http://source:9600/metrics"
              }
            },
            "status": "running",
            "restartPolicy": "always",
            "version": "1.0"
          }
        }
      }
```
