Docker image available here: [arlotito/edge-sim-sink](https://hub.docker.com/repository/docker/arlotito/edge-sim-sink) 

![for easy](https://img.shields.io/docker/v/arlotito/edge-sim-sink)

# Overview
An IoT Edge module:
* listening to 1 or more inputs
* measuring the per-input rate and other stats
* optionally echoing the inputs to a single output for cascading modules

## Parameters
Parameters are set via ENV vars:

| Variable | Description | Type |
|----------|:-------------:|------:|
| ECHO | Echo input to output | true or false |
| LOG_MSG | Log each message stats to console | true or false |
| LOG_BODY | Log message body | true or false |
| LOG_HIST | Log burst histogram to console | true or false |
| PERIOD | Calculation period (milliseconds) of the rate | Text or number |
| INPUT_CSV | comma separated list of desired inputs | comma separated values  |

Example:
```json
    "sink": {
        "version": "1.0",
        "type": "docker",
        "status": "running",
        "restartPolicy": "always",
        "settings": {
            "image": "arlotito/edge-benchmark-sink:0.4.4-amd64",
            "createOptions": "{}"
        },
        "env": {
            "LOG_MSG": {
            "value": "false"
            },
            "LOG_BODY": {
            "value": "false"
            },
            "PERIOD": {
            "value": "5000"
            },
            "ECHO": {
            "value": "true"
            },
            "INPUT_CSV": {
            "value": "input1,input2"
            }
        }
    }
```
            

## Docker image
image URI:
* arlotito/edge-benchmark-sink:0.4.4-amd64
* arlotito/edge-benchmark-sink:latest-amd64