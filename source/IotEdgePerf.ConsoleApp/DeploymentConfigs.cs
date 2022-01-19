
using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace IotEdgePerf.ConsoleApp
{

    internal static class EdgeConfigurations
    {
        public static ConfigurationContent GetBaseConfigurationContent(string transmitterAcrUri, 
                int maxUpstreamBatchSize,
                string logAnalyticsWorkspaceId,
                string logAnalyticsKey,
                string logAnalyticsIoTResourceId,
                bool addLogAnalytics = false)
        {
            ConfigurationContent content = new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    ["$edgeAgent"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeAgentConfiguration(transmitterAcrUri, 
                            maxUpstreamBatchSize, 
                            logAnalyticsWorkspaceId, 
                            logAnalyticsKey, 
                            logAnalyticsIoTResourceId,
                            addLogAnalytics)
                    },
                    ["$edgeHub"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeHubConfiguration()
                    },
                    ["source"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetTwinConfigurationTransmitter()
                    }
                }
            };


            if(addLogAnalytics)
            {
               
            }
            
            return content;
            
        }

        private static object GetTwinConfigurationTransmitter()
        {
            var desiredProperties = new
            {
                config = new 
                {
                    autoStart = false,
                    burstLength = 1000,
                    burstWait = 7000,
                    burstNumber = 1,
                    targetRate = 100,
                    payloadLength = 1024,
                    waitBeforeStart = 0,
                    batchSize = 1,
                    logMsg = false,
                    logBurst = true,
                    logHist = false,
                    rateCalcPeriod = 5000
                }
            };

            return desiredProperties;
        }

        private static object GetEdgeHubConfiguration()
        {
            var desiredProperties = new
            {
                schemaVersion = "1.0",
                routes = new Dictionary<string, string>
                {
                    ["route1"] = "FROM /messages/* INTO $upstream",
                },
                storeAndForwardConfiguration = new
                {
                    timeToLiveSecs = 7200
                }
            };
            return desiredProperties;
        }

        private static object GetEdgeAgentConfiguration(
            string transmitterUri, 
            int maxUpstreamBatchSize,
            string logAnalyticsWorkspaceId,
            string logAnalyticsKey,
            string logAnalyticsIoTResourceId,
            bool addLogAnalytics = false)
        {
            object modules;
            if(addLogAnalytics)
            {
                modules = new
                {
                    source = new
                    {
                        version = "1.0",
                        type = "docker",
                        status = "running",
                        restartPolicy = "on-failure",
                        settings = new
                        {
                            image = transmitterUri,
                            createOptions = "{}"
                        }
                    },
                    IoTEdgeMetricsCollector = new
                    {
                        version = "1.0",
                        type = "docker",
                        status = "running",
                        restartPolicy = "on-failure",
                        settings = new
                        {
                            image = "mcr.microsoft.com/azureiotedge-metrics-collector:1.0",
                            createOptions = "{}"
                        },
                        env = new 
                        {
                            ResourceId = new
                            {
                                 value = logAnalyticsIoTResourceId
                            },
                            UploadTarget = new
                            {
                                 value = "AzureMonitor"
                            },
                            LogAnalyticsWorkspaceId = new
                            {
                                 value = logAnalyticsWorkspaceId
                            },
                            LogAnalyticsSharedKey = new
                            {
                                 value = logAnalyticsKey
                            },
                            MetricsEndpointsCSV = new
                            {
                                 value = "http://edgeAgent:9600/metrics, http://edgeHub:9600/metrics"
                            },
                        }
                    }
                };
            }
            else
            {
                modules = new
                {
                    transmitter = new
                    {
                        version = "1.0",
                        type = "docker",
                        status = "running",
                        restartPolicy = "on-failure",
                        settings = new
                        {
                            image = transmitterUri,
                            createOptions = "{}"
                        }
                    }
                };
            }

            var desiredProperties = new
            {
                schemaVersion = "1.0",
                runtime = new
                {
                    type = "docker",
                    settings = new
                    {
                        loggingOptions = string.Empty
                    }
                },
                systemModules = new
                {
                    edgeAgent = new
                    {
                        type = "docker",
                        settings = new
                        {
                            image = "mcr.microsoft.com/azureiotedge-agent:1.2",
                            createOptions = "{}"
                        }
                    },
                    edgeHub = new
                    {
                        type = "docker",
                        status = "running",
                        restartPolicy = "always",
                        settings = new
                        {
                            image = "mcr.microsoft.com/azureiotedge-hub:1.2",
                            createOptions = "{\"HostConfig\":{\"Binds\":[\"/iotedge:/tmp/edgeHub\"]}}"
                        },
                        env = new 
                        {
                            RuntimeLogLevel = new
                            {
                                 value = "info"
                            },
                            MessageCleanupIntervalSecs =  new
                            {
                                 value = "7200"
                            },
                            MaxUpstreamBatchSize = new
                            {
                                value = maxUpstreamBatchSize
                            },
                            storageFolder = new
                            {
                                value = "/iotedge"
                            }
                        }

                    }
                },
                modules = modules
            };
            return desiredProperties;
        }

    }
}

