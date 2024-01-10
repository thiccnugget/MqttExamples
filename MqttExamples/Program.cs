using MQTTnet.Client;
using MQTTnet;

namespace MqttExamples
{
    public class Program
    {
        public async static Task Main()
        {

            string server = "localhost";
            int serverPort = 8883;
            string topic = "hello/world";
            string certFolder = "./Certs/";
            string CaCert = $"{certFolder}/ca.crt";


            #region PUBLISHER

            string pubName = "client";
            string pubCert = $"{certFolder}/{pubName}.crt";
            string pubKey = $"{certFolder}/{pubName}.key";

            //Create MQTT Client
            var pubClient = new MqttFactory().CreateMqttClient();

            //Configure the client using the custom method
            var pubClientOptions = MqttConfigurator.CreateTLSConfig(server, serverPort, pubName, pubCert, pubKey, null, CaCert, true);

            //Connect to the server and save the status code inside this variable
            var pubConnAck = await pubClient.ConnectAsync(pubClientOptions);

            //Write the status code in console
            Console.WriteLine($"{pubName} Connected: {pubClient.IsConnected} with CONNACK: {pubConnAck.ResultCode}");

            #endregion


            //-------------------------------------------------------------------------------

            #region SUBSCRIBER

            string subName = "sub";
            string subCert = $"{certFolder}/{subName}.crt";
            string subKey = $"{certFolder}/{subName}.key";


            //MQTT Client Creation
            var subClient = new MqttFactory().CreateMqttClient();

            //MQTT Message Received Action Registration
            subClient.ApplicationMessageReceivedAsync += async m => await Console.Out.WriteAsync($"Received message on topic: '{m.ApplicationMessage.Topic}' with content: '{m.ApplicationMessage.ConvertPayloadToString()}'\n\n");

            //Configuration and Connection to the MQTT server
            var subClientOptions = MqttConfigurator.CreateTLSConfig(server, serverPort, subName, subCert, subKey, null, null, false);

            //Connect and save the connection status inside this variable
            var subConnAck = await subClient.ConnectAsync(subClientOptions);

            //Write this line in console to check if the client has succesfully connected
            Console.WriteLine($"{subName} Connected: {subClient.IsConnected} with CONNACK: {subConnAck.ResultCode}");

            //Subscribe to the specified topic
            var suback = await subClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);

            //Write in console all topics the client is subscribed to
            suback.Items.ToList().ForEach(s => Console.WriteLine($"subscribed to '{s.TopicFilter.Topic}' with '{s.ResultCode}'\n"));

            #endregion

            #region MAIN_FUNCTION
            //Cancellation token with 10 seconds validity
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                //Publish to the topic and save the status code inside this variable
                var puback = await pubClient.PublishStringAsync(topic, "hello world!");

                //Write the status code in console
                Console.WriteLine(puback.ReasonCode);

                //Wait 2 seconds
                await Task.Delay(2000);
            }
            #endregion
        }
    }
}

