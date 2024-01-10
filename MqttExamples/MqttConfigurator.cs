using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client;


namespace MqttExamples
{
    public static class MqttConfigurator
    {

        /// <summary>
        /// This method returns an MqttClientOptions configuration to connect MQTT clients to an MQTT Broker using TLS and authentication certificates
        /// </summary>
        /// <param name="ServerAddress">Server FQDN / IP Address</param>
        /// <param name="Port">Server MQTT Port</param>
        /// <param name="ClientId">Identifies a client in the server</param>
        /// <param name="CaCertPath">Path for the CertificateAuthority certificate</param>
        /// <param name="ClientCertPath">Path for the client authentication certificate</param>
        /// <param name="ClientKeyPath">Path for the client public key</param>
        /// <param name="ClientCertPassword">Password to decrypt a password-protected client certificate</param>
        /// <returns></returns>
        public static MqttClientOptions CreateTLSConfig(string ServerAddress, int Port, string ClientId, string ClientCertPath, string ClientKeyPath, string? ClientCertPassword = null, string? CaCertPath = null, bool ValidateCACertificateChain = true)
        {
            //Throw exception if No CA cert is passed but CA chain validation is on (true)
            if (ValidateCACertificateChain && CaCertPath is null)
                throw new ArgumentNullException(nameof(CaCertPath), "\nCaCertPath must be a valid path to a CertificateAutority cert file when VerifyCACertificateChain is on. " +
                                                                    "Disable VerifyCACertificateChain or specify a CA file path. ");


            //Create new options builder and pass server address and port, client ID
            //(Client ID usually matches with username or cert Common Name [CN], double check your server config if this is problematic)
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(ServerAddress, Port)
                .WithClientId(ClientId);

            //Create new TLS options object
            MqttClientTlsOptions tlsParams = new MqttClientTlsOptions();
            tlsParams.UseTls = true;

            if (ValidateCACertificateChain)
            {
                //Extract the CA cert from the given path
                X509Certificate2 caCert = new X509Certificate2(File.ReadAllBytes(CaCertPath!));

                //Trust the given CA cert
                tlsParams.CertificateValidationHandler = (certContext) =>
                {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.VerificationTime = DateTime.Now;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 0); //no expiry
                    chain.ChainPolicy.CustomTrustStore.Add(caCert); //add the CA cert to the trust store
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    return chain.Build(new X509Certificate2(certContext.Certificate));
                };
            }
            else
            {
                //If no validation on the CA cert is done, just consider the cert valid
                // -- INSECURE, USE THIS FOR TESTING ONLY --
                tlsParams.CertificateValidationHandler = _ => true;
            }

            //Create a new collection of certificates
            X509Certificate2Collection clientCerts = new X509Certificate2Collection();

            //Combine Client Certificate and Client Key, eventually passing a password if the certificate is encrypted
            X509Certificate2 clientCert =
                ClientCertPassword is null
                ? X509Certificate2.CreateFromPemFile(ClientCertPath, ClientKeyPath)
                : X509Certificate2.CreateFromEncryptedPemFile(ClientCertPath, ClientCertPassword, ClientKeyPath);

            //Export the combined certificate to PFX format, then add it to the cert collection
            var clientCertPFX = new X509Certificate2(clientCert.Export(X509ContentType.Pfx));
            clientCerts.Add(clientCertPFX);

            //Provide the client certificate to the server using the default certificates provider
            tlsParams.ClientCertificatesProvider = new DefaultMqttCertificatesProvider(clientCerts);

            //Add this TLS configuration to client options
            mqttClientOptions.WithTlsOptions(tlsParams);

            //return this configuration
            return mqttClientOptions.Build();
        }
    }
}
