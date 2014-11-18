using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LogentriesCore.Net
{
    class LeClient
    {
        // Logentries API server address. 
        protected const String LeApiUrl = "api.logentries.com";

        // Port number for token logging on Logentries API server. 
        protected const int LeApiTokenPort = 10000;

        // Port number for TLS encrypted token logging on Logentries API server 
        protected const int LeApiTokenTlsPort = 20000;

        // Port number for HTTP PUT logging on Logentries API server. 
        protected const int LeApiHttpPort = 80;

        // Port number for SSL HTTP PUT logging on Logentries API server. 
        protected const int LeApiHttpsPort = 443;

        // Logentries API server certificate. 
        protected static readonly X509Certificate2 LeApiServerCertificate =
            new X509Certificate2(Encoding.UTF8.GetBytes(
@"-----BEGIN CERTIFICATE-----
MIIE3jCCA8agAwIBAgICGbowDQYJKoZIhvcNAQELBQAwZjELMAkGA1UEBhMCVVMx
FjAUBgNVBAoTDUdlb1RydXN0IEluYy4xHTAbBgNVBAsTFERvbWFpbiBWYWxpZGF0
ZWQgU1NMMSAwHgYDVQQDExdHZW9UcnVzdCBEViBTU0wgQ0EgLSBHNDAeFw0xNDEw
MjkxMjI5MzJaFw0xNjA5MTQwODE3MzlaMIGWMRMwEQYDVQQLEwpHVDAzOTM4Njcw
MTEwLwYDVQQLEyhTZWUgd3d3Lmdlb3RydXN0LmNvbS9yZXNvdXJjZXMvY3BzIChj
KTEyMS8wLQYDVQQLEyZEb21haW4gQ29udHJvbCBWYWxpZGF0ZWQgLSBRdWlja1NT
TChSKTEbMBkGA1UEAxMSYXBpLmxvZ2VudHJpZXMuY29tMIIBIjANBgkqhkiG9w0B
AQEFAAOCAQ8AMIIBCgKCAQEAyvDKhaiboZS5GHaZ7HBsidUBJoBu1YqMgUxvFohv
xppf5QqjjDP4knjKyC3K8t7cMTFem1CXHA03AW0nImy2cbDcWhr7MpTr5J90e3Ld
neWfBiFNStzjaE9jhdWDvu0ctVact1TIQgYfSAlRMEKW+OuaUwq3dEJNRJNzdrzE
aefQN7c4e2IgTuFvU9p7Qzifiq9Qu1VoSSDK3lxZiQuChWtd4sGYhqqjbkkMRvQ/
pRdiJ0gcFtGaqZLaj3Op+poz40iOiubWB4U8iOHiSjoGdRVi0LJKUeiSRw9lRO+1
qbj4g9ASZU+g7XugZn5GQvrR8E6ha5nZHEdDTI8JiEHXLwIDAQABo4IBYzCCAV8w
HwYDVR0jBBgwFoAUC1Dsd+8qm//sA6EK/63G5CoYxz4wVwYIKwYBBQUHAQEESzBJ
MB8GCCsGAQUFBzABhhNodHRwOi8vZ3Uuc3ltY2QuY29tMCYGCCsGAQUFBzAChhpo
dHRwOi8vZ3Uuc3ltY2IuY29tL2d1LmNydDAOBgNVHQ8BAf8EBAMCBaAwHQYDVR0l
BBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMB0GA1UdEQQWMBSCEmFwaS5sb2dlbnRy
aWVzLmNvbTArBgNVHR8EJDAiMCCgHqAchhpodHRwOi8vZ3Uuc3ltY2IuY29tL2d1
LmNybDAMBgNVHRMBAf8EAjAAMFoGA1UdIARTMFEwTwYKYIZIAYb4RQEHNjBBMD8G
CCsGAQUFBwIBFjNodHRwczovL3d3dy5nZW90cnVzdC5jb20vcmVzb3VyY2VzL3Jl
cG9zaXRvcnkvbGVnYWwwDQYJKoZIhvcNAQELBQADggEBAGL2wkx4Gk99EAcW0ClG
sCVFUbZ/DW2So0c5MjKkfFIGdH4a++x9eTNi28GoeF6YF2S8tOKS4fHHHxby4Fvn
ToUp4yR3Z3zAwNFULC1Gc+1kaV0/6k99LuiKNlIU7CHocSjQs7zvmc85l152lrAL
pzodvnfOn8rjUZvGOi2hb8VC7ZUSQCD9NJNNexF6G4dYc2TBjCD5xrhYXNcYCDXu
TGtvFnmBzFIO06IjqPWUFnerZxkktHf63PCB+xTxRWtDc84K91jmc+u7k/yY5wdf
aigW0/FPgSXR+as3fD1SSLuIgHynDdsUYLtCdbqiIRpZc/cmXzJI0bzhzpgGDPcn
81I=
-----END CERTIFICATE-----"));

        // Creates LeClient instance. If do not define useServerUrl and/or useOverrideProt during call
        // LeClient will be configured to work with api.logentries.com server; otherwise - with
        // defined server on defined port.
        public LeClient(bool useHttpPut, bool useSsl, bool useDataHub, String serverAddr, int port)
        {
            
            // Override port number and server address to send logs to DataHub instance.
            if (useDataHub)
            {
                m_UseSsl = false; // DataHub does not support receiving log messages over SSL for now.
                m_TcpPort = port;
                m_ServerAddr = serverAddr;
            }
            else
            {
                m_UseSsl = useSsl;

                if (!m_UseSsl)
                    m_TcpPort = useHttpPut ? LeApiHttpPort : LeApiTokenPort;
                else
                    m_TcpPort = useHttpPut ? LeApiHttpsPort : LeApiTokenTlsPort;
            }            
        }

        private bool m_UseSsl = false;
        private int m_TcpPort;
        private TcpClient m_Client = null;
        private Stream m_Stream = null;
        private SslStream m_SslStream = null;
        private String m_ServerAddr = LeApiUrl; // By default m_ServerAddr points to api.logentries.com if useDataHub is not set to true.

        private Stream ActiveStream
        {
            get
            {
                return m_UseSsl ? m_SslStream : m_Stream;
            }
        }

        public void Connect()
        {
            m_Client = new TcpClient(m_ServerAddr, m_TcpPort);
            m_Client.NoDelay = true;

            m_Stream = m_Client.GetStream();

            if (m_UseSsl)
            {
                m_SslStream = new SslStream(m_Stream, false, (sender, cert, chain, errors) => cert.GetCertHashString() == LeApiServerCertificate.GetCertHashString());
                m_SslStream.AuthenticateAsClient(m_ServerAddr);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            ActiveStream.Write(buffer, offset, count);
        }

        public void Flush()
        {
            ActiveStream.Flush();
        }

        public void Close()
        {
            if (m_Client != null)
            {
                try
                {
                    m_Client.Close();
                }
                catch
                {
                }
            }
        }
    }
}
