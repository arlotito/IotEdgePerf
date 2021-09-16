Docker image available here: [arlotito/edge-sim-source](https://hub.docker.com/repository/docker/arlotito/edge-sim-source) 

![for easy](https://img.shields.io/docker/v/arlotito/edge-sim-source)

# Overview
An IoT Edge module to generate parametric traffic:
![](/images/source-diagram.png)

## Parameters
Parameters are set via ENV vars:

| Variable | Description | Type |
|----------|:-------------:|------:|
| BURST_LENGTH | Number of messages in a burst | Text or number | 
| BURST_WAIT | Wait time (milliseconds) between bursts | Text or number |
| BURST_NUMBER | Number of bursts to be sent | Text or number |
| TARGET_RATE | Target rate (msg/s) within the burst | Text or number |
| MESSAGE_PAYLOAD_LENGTH | Payload size (will be filled with a random string of given length) | Text or number |
| START_WAIT | Wait before starting (milliseconds) | Text or number |
| LOG_MSG | Log each message stats to console | true or false |
| LOG_BURST | Log each burst stats to console | true or false |
| LOG_HIST | Log burst histogram to console | true or false |
| RATE_CALC_PERIOD | Calculation period of rate (milliseconds) | Text or number |

## Docker image
image URI: 
* arlotito/edge-benchmark-source:latest-amd64

## Message structure and sample
```csharp
    public class MessageDataPoint
    {
        // timestamp
        public long ts;
        
        // msg counter
        public int counter;
        
        // total msg to be sent
        public int total;

        // payload (random content)
        public string payload;
    }
```

Below a sample message with
* BURST_LENGTH = 1500
* MESSAGE_PAYLOAD_LENGTH = 1024

```json
{
  "body": {
    "ts": 1631714603334,
    "counter": 1366,
    "total": 1500,
    "payload": "5kF5Q3QK1L5yPlbBOqNLoYIXm1THfYrQGzoo1QvnzPJOCkOCdLDmjFcgbrSYjJpdeosa6wK84kVZO4VCnQoUUErAWHNJ7WDqjquX1xFqBFr8uCShWQvTeVIZ7rDyjAAihoziK7KzL8SPXsQTzpxrltLOhUaJuRGAEJbFNtXYG5HgC3CObu83NT7xclAzhAE31SKOQLGGSYlpmoMoSLsASMxvLomlsh0F1Optsy8Pn2lysSLyYWhWRElofZCmieXvifyX9c6dNdWal8D3dyMW9dywnAOSwVqIfzQMdxy6aDJyrLChWIIuRAvEqBu8Nnr1wN4vr0Zq9Q66UAEc8r5nN61eEMgcC0swVSBdQQzgscuBEzuq6SAgrEozWZrfT7nJhJkC8kLr2Z5uMzaOczfuzMERoMH0jTMuykd5PrsWGToSI3ng9dDec3TVESYwdUHiMK6lWf9uHJnghwIuik1FQsS5QH06T52vLFF87BKeeTWTQxxSJnN2dCSe2BUOoCaLQ1VcqKxhaXl7W9odUixps9SJxlG5r5zc2KCZbVl9zqcGbVI2W04GOudQiMoFE1zfhJEL6PB87y0ChLscdNj0BtNHrv0Pqf5806jSQ66Hz5Ja4asXeMm0S9ey57dKYvSb21MkmaLgRbBFjwMRDcvd3Ep0cuWSdvsDnrhCOHvEAGj08YOJvMH5AyiyMVI1D3Dfs7c2OJC5rrcxfy1EgMgQnxDyJt0293AHCDIA6W4mJMNSWvTXdEV0Ww154nteSJ5yrVubq0HKR4GbjucXadY5iJuQLMGSmaTAQZlAB1qn6KibqaWKLVEzvAPikePduzh1lGLJJbWiwv9XkSjv06GRVdPQKCZhlM2Hnr0jPMAu9fD9A17S4enysQgH149GBz7j31t3bAgo5WR0aPPSFHmHjEZXLtsUN3sKidGJHE04792V8prloqB3Mz9ycWssRLaBnHR6JGKBRsKkLDZfBGpg2y7teRqvVHmvi1s0YubigAmdpZnY7NqKg3JyKic2ZZpJ"
  },
  "enqueuedTime": "Wed Sep 15 2021 16:03:23 GMT+0200 (Central European Summer Time)",
  "properties": {}
}
```

## Sample output
show logs

## Prometheus metrics
show prometheus metrics

## Sample deployment manifest
```bash
{
  "modulesContent": {
    "$edgeAgent": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "runtime": {
          "type": "docker",
          "settings": {
            "minDockerVersion": "v1.25",
            "loggingOptions": "",
            "registryCredentials": {
              "arlotito": {
                "username": "$CONTAINER_REGISTRY_USERNAME",
                "password": "$CONTAINER_REGISTRY_PASSWORD",
                "address": "$CONTAINER_REGISTRY_URL"
              }
            }
          }
        },
        "systemModules": {
          "edgeAgent": {
            "type": "docker",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-agent:1.2",
              "createOptions": "{}"
            }
          },
          "edgeHub": {
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-hub:1.2",
              "createOptions": "{}"
            },
            "env": {
              
            }
          }
        },
        "modules": {
          "source": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "arlotito/edge-benchmark-source:latest-amd64",
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
                "value": "$LOG_ANALYTICS_RESOURCE_ID"
              },
              "UploadTarget": {
                "value": "AzureMonitor"
              },
              "LogAnalyticsWorkspaceId": {
                "value": "$LOG_ANALYTICS_WORKSPACE_ID"
              },
              "LogAnalyticsSharedKey": {
                "value": "$LOG_ANALYTICS_SHARED_KEY"
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
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "upstream": "FROM /messages/modules/source/outputs/* INTO $upstream"
        },
        "storeAndForwardConfiguration": {
          "timeToLiveSecs": 7200
        }
      }
    }
  }
}
```

The .env file:
```bash
CONTAINER_REGISTRY_USERNAME=<..>
CONTAINER_REGISTRY_PASSWORD=<..>
CONTAINER_REGISTRY_URL=<..>
LOG_ANALYTICS_RESOURCE_ID=<..>
LOG_ANALYTICS_WORKSPACE_ID=<..>
LOG_ANALYTICS_SHARED_KEY=<..>
```
