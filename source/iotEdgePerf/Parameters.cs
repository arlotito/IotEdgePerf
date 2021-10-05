// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;
using IoTEdgePerf.Shared;

namespace IotEdgePerf.ConsoleApp
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
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
            "iot-conn-string",
            Required = false,
            Default = "",
            HelpText = "The connection string to the iot hub.\nIf you prefer, you can use the env var IOT_CONN_STRING instead.")]
        public string IotHubConnectionString { get; set; }

        [Option(
            "device-id",
            Required = true,
            Default = "",
            HelpText = "The device id")]
        public string DeviceId { get; set; }

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
    }

    partial class Program
    {
        private static void GetConfig(string[] args)
        {
            Parameters _parameters = new Parameters();

            // Parse application parameters
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    _parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            EventHubName = Environment.GetEnvironmentVariable("EH_NAME");
            if (!string.IsNullOrEmpty(_parameters.EventHubName))
            {
                EventHubName = _parameters.EventHubName;
            }

            EventHubConnectionString = Environment.GetEnvironmentVariable("EH_CONN_STRING");
            if (!string.IsNullOrEmpty(_parameters.EventHubConnectionString))
            {
                EventHubConnectionString = _parameters.EventHubConnectionString;
            }

            IotHubConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            if (!string.IsNullOrEmpty(_parameters.IotHubConnectionString))
            {
                IotHubConnectionString = _parameters.IotHubConnectionString;
            }

            // check if EH info is provided
            if (string.IsNullOrWhiteSpace(EventHubConnectionString)
                || string.IsNullOrWhiteSpace(EventHubName))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            double.TryParse(_parameters.Timeout, out TimeoutInterval);
            ShowMsg = _parameters.ShowMsg;
            DeviceId = _parameters.DeviceId;

            transmitterConfig = new TransmitterConfigData {
                enable = true,
                burstLength=_parameters.burstLength,
                burstWait=_parameters.burstWait,
                burstNumber=_parameters.burstNumber,
                targetRate=_parameters.targetRate,
                payloadLength=_parameters.payloadLength,
                batchSize=_parameters.batchSize,
                logMsg=false,
                logBurst=true,
                logHist=false,
                waitBeforeStart=_parameters.waitBeforeStart,
                rateCalcPeriod=5000
            };
        }
    }
}
