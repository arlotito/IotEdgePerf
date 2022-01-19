// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace IotEdgePerf.ConsoleApp
{

    internal abstract class BaseOptions
    {
         [Option(
            "iot-conn-string",
            Required = false,
            Default = "",
            HelpText = "The connection string to the iot hub.\nIf you prefer, you can use the env var IOT_CONN_STRING instead.")]
        public string IotHubConnectionString { get; set; }

         [Option(
            "device-id",
            Required = false,
            Default = "",
            HelpText = "The device id. \nIf you prefer, you can use the env var DEVICE_ID instead.")]
        public string DeviceId { get; set; }



    }

    [Verb("run")]
    /// <summary>
    /// Parameters for the application in Run / execute mode
    /// </summary>
    internal class RunOptions: BaseOptions
    {
        [Option(
            'n',
            "ehName",
            Required = false,
            Default = "",
            HelpText = "The event hub-compatible name of your IoT Hub instance.\nIf you prefer, you can use the env var EH_NAME instead. \nUse `az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}` to fetch via the Azure CLI.")]
        public string EventHubName { get; set; }

        [Option(
            "eh-conn-string",
            Required = false,
            Default = "",
            HelpText = "The connection string to the event hub-compatible endpoint.\nIf you prefer, you can use the env var EH_CONN_STRING instead. Use the Azure portal to get this parameter.")]
        public string EventHubConnectionString { get; set; }

       
        [Option(
            't',
            "timeout",
            Required = false,
            Default = "20000",
            HelpText = "If a new message is not received within this timeout, the app exits")]
        public string Timeout { get; set; }

        [Option(
            "show-msg",
            Required = false,
            Default = false,
            HelpText = "Show received messages")]
        public bool ShowMsg { get; set; }

        //-----------
        [Option(
            "burst-length",
            Required = false,
            Default = 1000,
            HelpText = "Number of messages in a single burst.")]
        public int burstLength { get; set; }

        [Option(
            "burst-number",
            Required = false,
            Default = 1,
            HelpText = "Number of bursts.")]
        public int burstNumber { get; set; }

        [Option(
            "target-rate",
            Required = false,
            Default = 50,
            HelpText = "Target rate [msg/s]")]
        public int targetRate { get; set; }

        [Option(
            "payload-length",
            Required = false,
            Default = 1024,
            HelpText = "Number of bytes in the message.")]
        public int payloadLength { get; set; }

        [Option(
            "burst-wait",
            Required = false,
            Default = 7000,
            HelpText = "Millisencods to wait before next burst.")]
        public int burstWait { get; set; }

        [Option(
            "burst-before-start",
            Required = false,
            Default = 0,
            HelpText = "Millisencods to wait before starting.")]
        public int waitBeforeStart { get; set; }

        [Option(
            "batch-size",
            Required = false,
            Default = 1,
            HelpText = "Batch size")]
        public int batchSize { get; set; }

        [Option(
            'o',
            "output-csv-file",
            Required = false,
            Default = "",
            HelpText = "Output file for test results. CSV. If already existing, results will be appended.")]
        public string csvOutputFile { get; set; }

        [Option(
            'l',
            "test-label",
            Required = false,
            Default = "test",
            HelpText = "Custom label that will be added to the output results.")]
        public string TestLabel { get; set; }
    }

    [Verb("deploy")]
    internal class DeployOptions: BaseOptions
    {
        [Option(
            "image-uri",
            Required = true,
            HelpText = "Container Image URI and tag, for example 'arlotito/iotedgeperf-transmitter:0.5.0'")]
        public string ImageUri { get; set; }

        [Option(
            'b', 
            "batch-max-size",
            Required = false,
            Default = 200,
            HelpText = "(optional) this value will be assigned to edgeHub's MaxUpstreamBatchSize env var.")]
        public int MaxUpstreamBatchSize { get; set; }

        [Option(
            "log-a-workspaceid",
            Required = false,
            HelpText = "(optional) The Log Analytics Workspace ID")]
        public string LogAnalyticsWorkspaceId { get; set; }

        [Option(
            "log-a-iotresourceid",
            Required = false,
            HelpText = "(optional but required when adding Log Analytics) The IoT Hub resource ID")]
        public string LogAnalyticsIoTResourceId { get; set; }

        [Option(
            "log-a-key",
            Required = false,
            HelpText = "(required when adding Log Analytics) The shared key for Log Analytics")]
        public string LogAnalyticsSharedKey { get; set; }


    }

   
}
