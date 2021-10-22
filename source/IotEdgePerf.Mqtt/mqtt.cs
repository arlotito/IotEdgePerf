using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;


using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using IotEdgePerf.SecurityDaemonClient;

public delegate void DirectMethodHandler(string payload);

namespace IotEdgePerf.Mqtt
{
    public class Client
    {
        string _username = "";
        string _password = "";
        string _url = "";
        string _clientId = "";
        string _topic = "";

        public event DirectMethodHandler DirectMethodReceived;

        IMqttClient _mqttClient;

        X509Certificate2[] _caCrt;

        public Client()
        {
        }
        
        async public Task Init()
        {
            //
            var securityDaemonClient = new IotEdgePerf.SecurityDaemonClient.SecurityDaemonClient();

            //
            this._caCrt = await securityDaemonClient.GetTrustBundleAsync();

            //
            Console.WriteLine(securityDaemonClient.IotHubName);
            Console.WriteLine(securityDaemonClient.IotHubHostName);
            Console.WriteLine(securityDaemonClient.DeviceId);
            Console.WriteLine(securityDaemonClient.ModuleId);

            var SAStoken = await securityDaemonClient.GetModuleToken();
            Console.WriteLine(SAStoken);

            // MQTT
            // https://github.com/chkr1011/MQTTnet/wiki/Client#preparation
            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-module
            
            // <hubname>.azure-devices.net/{device_id}/{module_id}/?api-version=2018-06-30
            this._username = securityDaemonClient.IotHubHostName + "/" + 
                securityDaemonClient.DeviceId + "/" + securityDaemonClient.ModuleId + 
                "/?api-version=2018-06-30";   
            
            this._password = SAStoken;   // SAS token

            this._url = "edgeHub";        // 
            this._clientId = securityDaemonClient.DeviceId + "/" + securityDaemonClient.ModuleId;   // {device_id}/{module_id}

            // devices/{device_id}/modules/{module_id}/messages/events/
            this._topic = "devices/" + securityDaemonClient.DeviceId + "/modules/" + securityDaemonClient.ModuleId + "/messages/events/";

            Console.WriteLine(this._clientId);
            Console.WriteLine(this._username);
            Console.WriteLine(this._password);
            Console.WriteLine(this._topic);

            var factory = new MqttFactory();
            
            this._mqttClient = factory.CreateMqttClient();
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId(this._clientId)
                .WithTcpServer(this._url)
                .WithCredentials(this._username, this._password)
                .WithTls(parameters =>
                    {
                        parameters.UseTls = true;
                        parameters.SslProtocol = SslProtocols.Tls12;
                        parameters.CertificateValidationHandler = _ => true; //trust anything
                        parameters.AllowUntrustedCertificates = true;
                        parameters.IgnoreCertificateChainErrors = true;
                        parameters.IgnoreCertificateRevocationErrors = true;
                    }
                )
                .Build();
            //var quality = MqttQualityOfServiceLevel.AtLeastOnce;
            
            this._mqttClient.UseDisconnectedHandler(async e =>
            {
                Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await this._mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
                }
                catch
                {
                    Console.WriteLine("### RECONNECTING FAILED ###");
                }
            });

            _mqttClient.UseConnectedHandler(async e => 
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");
            });

            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                try
                {
                    Console.WriteLine(".");
                    string topic = e.ApplicationMessage.Topic;

                    if (string.IsNullOrWhiteSpace(topic) == false)
                    {
                        await HandleMessageReceived(e.ApplicationMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
            });

            // connect
            await this._mqttClient.ConnectAsync(options, CancellationToken.None);

            //subscribe AFTER connect
            // DM: $iothub/methods/POST/{method name}/?$rid={request id}
            await this._mqttClient.SubscribeAsync(
                new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("$iothub/methods/POST/#", MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
            );
            Console.WriteLine($"### SUBSCRIBED ###");
        }

        private async Task HandleMessageReceived(MqttApplicationMessage applicationMessage)
        {
            Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            Console.WriteLine($"+ Topic = {applicationMessage.Topic}");

            Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(applicationMessage.Payload)}");
            Console.WriteLine($"+ QoS = {applicationMessage.QualityOfServiceLevel}");
            Console.WriteLine($"+ Retain = {applicationMessage.Retain}");
            Console.WriteLine();

            string methodName = "Start";
            string dmTopic = String.Format($"$iothub/methods/POST/{methodName}/?$rid="); 
            if (applicationMessage.Topic.Contains(dmTopic))
            {
                if (DirectMethodReceived != null)
                    DirectMethodReceived.Invoke(Encoding.UTF8.GetString(applicationMessage.Payload));
                
                string rid = applicationMessage.Topic.Replace(dmTopic, "");
                Console.WriteLine(rid);

                //$iothub/methods/res/{status}/?$rid={request id}
                int status = 0;
                string responseTopic = String.Format($"$iothub/methods/res/{status}/?$rid={rid}");
                Console.WriteLine(responseTopic);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(responseTopic)
                    .WithPayload("")
                    //.WithExactlyOnceQoS()
                    //.WithRetainFlag()
                    .Build();

                await this._mqttClient.PublishAsync(message, CancellationToken.None); 
            }
        }

        async public Task SendEventAsync(string moduleOutput, string payload)
        {
            //Console.WriteLine(this.topic);
            //Console.WriteLine(str);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(this._topic)
                .WithPayload(payload)
                //.WithExactlyOnceQoS()
                //.WithRetainFlag()
                .Build();

            await this._mqttClient.PublishAsync(message, CancellationToken.None);
        }

        

    }
}