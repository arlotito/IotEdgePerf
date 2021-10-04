// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace eh_consumer
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
            'c',
            "ehConnString",
            Required = false,
            Default = "",
            HelpText = "The connection string to the event hub-compatible endpoint.\nIf you prefer, you can use the env var EH_CONN_STRING instead. Use the Azure portal to get this parameter.")]
        public string EventHubConnectionString { get; set; }

        [Option(
            't',
            "timeout",
            Required = false,
            Default = "60000",
            HelpText = "If a new message is not received within this timeout, the app exits")]
        public string Timeout { get; set; }

        [Option(
            "show-msg",
            Required = false,
            Default = false,
            HelpText = "Show received messages")]
        public bool ShowMsg { get; set; }
    }
}
