using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace LogentriesCore.Net
{
    using System.Collections.Concurrent;

    public class AsyncLogger
    {
        #region Constants

        // Current version number.
        protected const String Version = "2.9.0";

        // Size of the internal event queue. 
        protected const int QueueSize = 32768;

        // Limit on individual log length ie. 2^16
        protected const int LOG_LENGTH_LIMIT = 65536;

        // Limit on recursion for appending long logs to queue
        protected const int RECURSION_LIMIT = 32;

        // Minimal delay between attempts to reconnect in milliseconds. 
        protected const int MinDelay = 100;

        // Maximal delay between attempts to reconnect in milliseconds. 
        protected const int MaxDelay = 10000;

        // Appender signature - used for debugging messages. 
        protected const String LeSignature = "LE: ";

        // Legacy Logentries configuration names. 
        protected const String LegacyConfigTokenName = "LOGENTRIES_TOKEN";
        protected const String LegacyConfigAccountKeyName = "LOGENTRIES_ACCOUNT_KEY";
        protected const String LegacyConfigLocationName = "LOGENTRIES_LOCATION";

        // New Logentries configuration names.
        protected const String ConfigTokenName = "Logentries.Token";
        protected const String ConfigAccountKeyName = "Logentries.AccountKey";
        protected const String ConfigLocationName = "Logentries.Location";

        // Error message displayed when invalid token is detected. 
        protected const String InvalidTokenMessage = "\n\nIt appears your LOGENTRIES_TOKEN value is invalid or missing.\n\n";

        // Error message displayed when invalid account_key or location parameters are detected. 
        protected const String InvalidHttpPutCredentialsMessage = "\n\nIt appears your LOGENTRIES_ACCOUNT_KEY or LOGENTRIES_LOCATION values are invalid or missing.\n\n";

        // Error message deisplayed when queue overflow occurs. 
        protected const String QueueOverflowMessage = "\n\nLogentries buffer queue overflow. Message dropped.\n\n";

        // Newline char to trim from message for formatting. 
        protected static char[] TrimChars = { '\r', '\n' };

        /** Non-Unix and Unix Newline */
        protected static string[] posix_newline = { "\r\n", "\n" };

        /** Unicode line separator character */
        protected static string line_separator = "\u2028";

        // Restricted symbols that should not appear in host name.
        // See http://support.microsoft.com/kb/228275/en-us for details.
        private static Regex ForbiddenHostNameChars = new Regex(@"[/\\\[\]\""\:\;\|\<\>\+\=\,\?\* _]{1,}", RegexOptions.Compiled);

        #endregion

        #region Singletons

        // UTF-8 output character set. 
        protected static readonly UTF8Encoding UTF8 = new UTF8Encoding();

        // ASCII character set used by HTTP. 
        protected static readonly ASCIIEncoding ASCII = new ASCIIEncoding();

        //static list of all the queues the le appender might be managing.
        private static ConcurrentBag<BlockingCollection<string>> _allQueues = new ConcurrentBag<BlockingCollection<string>>();

        public readonly SettingsLookup SettingsLookup = SettingsLookupFactory.Create();

        /// <summary>
        /// Determines if the queue is empty after waiting the specified waitTime.
        /// Returns true or false if the underlying queues are empty.
        /// </summary>
        /// <param name="waitTime">The length of time the method should block before giving up waiting for it to empty.</param>
        /// <returns>True if the queue is empty, false if there are still items waiting to be written.</returns>
        public static bool AreAllQueuesEmpty(TimeSpan waitTime)
        {
            var start = DateTime.UtcNow;
            var then = DateTime.UtcNow;

            while (start.Add(waitTime) > then)
            {
                if (_allQueues.All(x => x.Count == 0))
                    return true;

                Thread.Sleep(100);
                then = DateTime.UtcNow;
            }

            return _allQueues.All(x => x.Count == 0);
        }
        #endregion

        public AsyncLogger()
        {
            Queue = new BlockingCollection<string>(QueueSize);
            ThreadCancellationTokenSource = new CancellationTokenSource();
            _allQueues.Add(Queue);

            WorkerThread = new Thread(new ThreadStart(Run));
        }

        #region Configuration properties

        private String m_Token = "";
        private String m_AccountKey = "";
        private String m_Location = "";
        public bool m_Debug = false;
        private bool m_UseHttpPut = false;
        private bool m_UseSsl = false;

        // Properties for defining location of DataHub instance if one is used.
        private bool m_UseDataHub = false; // By default Logentries service is used instead of DataHub instance.
        private String m_DataHubAddr = "";
        private int m_DataHubPort = 0;

        // Properties to define host name of user's machine and define user-specified log ID.
        private bool m_UseHostName = false; // Defines whether to prefix log message with HostName or not.
        private String m_HostName = ""; // User-defined or auto-defined host name (if not set in config. file)
        private String m_LogID = ""; // User-defined log ID to be prefixed to the log message.

        // Sets DataHub usage flag.
        public void setIsUsingDataHub(bool useDataHub)
        {
            m_UseDataHub = useDataHub;
        }

        public bool getIsUsingDataHab()
        {
            return m_UseDataHub;
        }

        // Sets DataHub instance address.
        public void setDataHubAddr(String dataHubAddr)
        {
            m_DataHubAddr = dataHubAddr;
        }

        public String getDataHubAddr()
        {
            return m_DataHubAddr;
        }

        // Sets the port on which DataHub instance is waiting for log messages.
        public void setDataHubPort(int port)
        {
            m_DataHubPort = port;
        }

        public int getDataHubPort()
        {
            return m_DataHubPort;
        }

        public void setToken(String token)
        {
            m_Token = token;
        }

        public String getToken()
        {
            return m_Token;
        }

        public void setAccountKey(String accountKey)
        {
            m_AccountKey = accountKey;
        }

        public string getAccountKey()
        {
            return m_AccountKey;
        }

        public void setLocation(String location)
        {
            m_Location = location;
        }

        public String getLocation()
        {
            return m_Location;
        }

        [Obsolete("No longer used. Flush always enabled")]
        public void setImmediateFlush(bool immediateFlush)
        {
            // Obsolete
        }

        [Obsolete("No longer used. Flush always enabled")]
        public bool getImmediateFlush()
        {
            return true;    // Obsolete
        }

        public void setDebug(bool debug)
        {
            m_Debug = debug;
        }

        public bool getDebug()
        {
            return m_Debug;
        }

        public void setUseHttpPut(bool useHttpPut)
        {
            m_UseHttpPut = useHttpPut;
        }

        public bool getUseHttpPut()
        {
            return m_UseHttpPut;
        }

        public void setUseSsl(bool useSsl)
        {
            m_UseSsl = useSsl;
        }

        public bool getUseSsl()
        {
            return m_UseSsl;
        }

        public void setUseHostName(bool useHostName)
        {
            m_UseHostName = useHostName;
        }

        public bool getUseHostName()
        {
            return m_UseHostName;
        }

        public void setHostName(String hostName)
        {
            m_HostName = hostName;
        }

        public String getHostName()
        {
            return m_HostName;
        }

        public void setLogID(String logID)
        {
            m_LogID = logID;
        }

        public String getLogID()
        {
            return m_LogID;
        }

        #endregion

        protected readonly BlockingCollection<string> Queue;
        protected Thread WorkerThread;
        protected CancellationTokenSource ThreadCancellationTokenSource;
        protected readonly Random Random = new Random();

        private LeClient LeClient = null;
        protected bool IsRunning = false;

        #region Protected methods

        protected virtual void Run()
        {
            try
            {
                // Open connection.
                ReopenConnection();

                string logMessagePrefix = String.Empty;

                if (m_UseHostName)
                {
                    // If LogHostName is set to "true", but HostName is not defined -
                    // try to get host name from Environment.
                    if (string.IsNullOrEmpty(m_HostName))
                    {
                        try
                        {
                            WriteDebugMessages("HostName parameter is not defined - trying to get it from System.Environment.MachineName");

                            string hostName;
#if NETSTANDARD1_3
                            hostName = System.Environment.GetEnvironmentVariable("COMPUTERNAME") ?? string.Empty;
                            if (string.IsNullOrEmpty(hostName))
                                hostName = System.Environment.GetEnvironmentVariable("HOSTNAME") ?? string.Empty;
                            if (string.IsNullOrEmpty(hostName))
                                throw new ArgumentNullException("HOSTNAME");
#else
                            hostName = System.Environment.MachineName;
#endif
                            m_HostName = "HostName=" + hostName + " ";
                        }
                        catch (Exception ex)
                        {
                            // Cannot get host name automatically, so assume that HostName is not used
                            // and log message is sent without it.
                            m_UseHostName = false;
                            WriteDebugMessages("Failed to get HostName parameter using System.Environment.MachineName. Log messages will not be prefixed by HostName", ex);
                        }
                    }
                    else
                    {
                        if (!CheckIfHostNameValid(m_HostName))
                        {
                            // If user-defined host name is incorrect - we cannot use it
                            // and log message is sent without it.
                            m_UseHostName = false;
                            WriteDebugMessages("HostName parameter contains prohibited characters. Log messages will not be prefixed by HostName");
                        }
                        else
                        {
                            m_HostName = "HostName=" + m_HostName + " ";
                        }
                    }
                }

                if (m_LogID != String.Empty)
                {
                    logMessagePrefix = m_LogID + " ";
                }

                if (m_UseHostName)
                {
                    logMessagePrefix += m_HostName;
                }

                // Flag that is set if logMessagePrefix is empty.
                bool isPrefixEmpty = (logMessagePrefix == String.Empty);

                StringBuilder finalLine = new StringBuilder();

                var cancellationToken = ThreadCancellationTokenSource.Token;

                // Send data in queue.
                while (!cancellationToken.IsCancellationRequested)
                {
                    // added debug here
                    WriteDebugMessages("Await queue data");

                    finalLine.Length = 0;

                    // Take data from queue.
                    var line = Queue.Take(cancellationToken);
                    //added debug message here
                    WriteDebugMessages("Queue data obtained");

                    // Replace newline chars with line separator to format multi-line events nicely.
                    foreach (String newline in posix_newline)
                    {
                        line = line.Replace(newline, line_separator);
                    }

                    // If m_UseDataHub == true (logs are sent to DataHub instance) then m_Token is not
                    // appended to the message.
                    if (!m_UseHttpPut && !m_UseDataHub)
                    {
                        finalLine.Append(this.m_Token);
                    }

                    // Add prefixes: LogID and HostName if they are defined.
                    if (!isPrefixEmpty)
                    {
                        finalLine.Append(logMessagePrefix);
                    }

                    finalLine.Append(line);
                    finalLine.Append('\n');

                    byte[] data = UTF8.GetBytes(finalLine.ToString());

                    // Send data, reconnect if needed.
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            //removed iff loop and added debug message
                            // Le.Client writes data
                            WriteDebugMessages("Write data");
                            this.LeClient.Write(data, 0, data.Length);
                            WriteDebugMessages("Write complete");
                        }
                        catch (IOException e)
                        {
                            WriteDebugMessages("IOException during write, reopen: ", e);
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            // Reopen the lost connection.
                            ReopenConnection();
                            continue;
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugMessages("Logentries asynchronous socket client was interrupted: ", ex);
            }
        }

        protected virtual void OpenConnection()
        {
            try
            {
                if (LeClient == null)
                {
                    // Create LeClient instance providing all needed parameters. If DataHub-related properties
                    // have not been overridden by log4net or NLog configurators, then DataHub is not used, 
                    // because m_UseDataHub == false by default.
                    LeClient = new LeClient(m_UseHttpPut, m_UseSsl, m_UseDataHub, m_DataHubAddr, m_DataHubPort);
                }

                LeClient.Connect();

                if (m_UseHttpPut)
                {
                    var header = String.Format("PUT /{0}/hosts/{1}/?realtime=1 HTTP/1.1\r\n\r\n", m_AccountKey, m_Location);
                    var headerBytes = ASCII.GetBytes(header);
                    LeClient.Write(headerBytes, 0, headerBytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new IOException("An error occurred while opening the connection.", ex);
            }
        }

        protected virtual void ReopenConnection()
        {
            WriteDebugMessages("ReopenConnection");
            CloseConnection();

            var cancellationToken = ThreadCancellationTokenSource.Token;

            var rootDelay = MinDelay;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    OpenConnection();
                    return;
                }
                catch (Exception ex)
                {
                    WriteDebugMessages(string.Format("Unable to connect to Logentries API at {0}:{1}", LeClient != null ? LeClient.ServerAddr : "null", LeClient != null ? LeClient.TcpPort : 0), ex);
                }

                rootDelay *= 2;
                if (rootDelay > MaxDelay)
                    rootDelay = MaxDelay;

                var waitFor = rootDelay + Random.Next(rootDelay);
                WriteDebugMessages(string.Format("Waiting {0} ms for retry", waitFor));

                cancellationToken.WaitHandle.WaitOne(waitFor);
            }
        }

        protected virtual void CloseConnection()
        {
            if (LeClient != null)
                LeClient.Close();
        }

        public static bool IsNullOrWhiteSpace(String value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }

        /* Retrieve configuration settings
         * Will check Enviroment Variable as the last fall back.
         * 
         */
        private string retrieveSetting(String name)
        {
            string settingStoreName;
            string settingValue = SettingsLookup.GetSettingValue(name, out settingStoreName);
            if (!string.IsNullOrEmpty(settingValue))
            {
                WriteDebugMessages(String.Format("Found setting {0} in {1}", name, settingStoreName));
                return settingValue;
            }

            WriteDebugMessages(String.Format("Unable to find Logentries Configuration Setting for {0}.", name));
            return null;
        }

        /*
         * Use CloudConfigurationManager with .NET4.0 and fallback to System.Configuration for previous frameworks.
         * 
         *       
         *       One issue is that there are two appsetting keys for each setting - the "legacy" key, such as "LOGENTRIES_TOKEN"
         *       and the "non-legacy" key, such as "Logentries.Token".  Again, I'm not sure of the reasons behind this, so the code below checks
         *       both the legacy and non-legacy keys, defaulting to the legacy keys if they are found.
         *       
         *       It probably should be investigated whether the fallback to ConfigurationManager is needed at all, as CloudConfigurationManager 
         *       will retrieve settings from appSettings in a non-Azure environment.
         */
        public virtual bool LoadCredentials()
        {
            if (!m_UseHttpPut)
            {
                if (GetIsValidGuid(m_Token))
                    return true;

                var configToken = retrieveSetting(LegacyConfigTokenName) ?? retrieveSetting(ConfigTokenName);

                if (!String.IsNullOrEmpty(configToken) && GetIsValidGuid(configToken))
                {
                    m_Token = configToken;
                    return true;
                }
                WriteDebugMessages(InvalidTokenMessage);
                return false;
            }

            if (m_AccountKey != "" && GetIsValidGuid(m_AccountKey) && m_Location != "")
                return true;

            var configAccountKey = retrieveSetting(LegacyConfigAccountKeyName) ?? retrieveSetting(ConfigAccountKeyName);

            if (!String.IsNullOrEmpty(configAccountKey) && GetIsValidGuid(configAccountKey))
            {
                m_AccountKey = configAccountKey;

                var configLocation = retrieveSetting(LegacyConfigLocationName) ?? retrieveSetting(ConfigLocationName);

                if (!String.IsNullOrEmpty(configLocation))
                {
                    m_Location = configLocation;
                    return true;
                }
            }

            WriteDebugMessages(InvalidHttpPutCredentialsMessage);
            return false;
        }

        private bool CheckIfHostNameValid(String hostName)
        {
            return !ForbiddenHostNameChars.IsMatch(hostName); // Returns false if reg.ex. matches any of forbidden chars.
        }

        protected virtual bool GetIsValidGuid(string guidString)
        {
            if (String.IsNullOrEmpty(guidString))
                return false;

            System.Guid newGuid = System.Guid.Empty;

            try
            {
#if NET35
                newGuid = new Guid(guidString);
#else
                if (!System.Guid.TryParse(guidString, out newGuid))
                    return false;
#endif
                return newGuid != System.Guid.Empty;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void WriteDebugMessages(string message, Exception ex)
        {
            if (!m_Debug)
                return;

            message = string.Concat(LeSignature, message, ex.ToString());

            Trace.WriteLine(message);
        }

        protected virtual void WriteDebugMessages(string message)
        {
            if (!m_Debug)
                return;

            message = LeSignature + message;

            Trace.WriteLine(message);
        }

        private void WriteDebugMessagesFormat<T>(string message, T arg0)
        {
            if (!m_Debug)
                return;

            WriteDebugMessages(string.Format(message, arg0));
        }

        #endregion

        #region Public Methods

        public virtual void AddLine(string line)
        {
            AddLineToQueue(line, RECURSION_LIMIT);
        }

        public void interruptWorker()
        {
            if (IsRunning)
            {
                try
                {
                    ThreadCancellationTokenSource.Cancel();
                    WorkerThread.Join(1000);
                }
                finally
                {
                    ThreadCancellationTokenSource = new CancellationTokenSource();
                    WorkerThread = new Thread(new ThreadStart(Run));
                    IsRunning = false;
                }
            }
        }

        public bool FlushQueue(TimeSpan waitTime)
        {
            var cancellationToken = ThreadCancellationTokenSource.Token;

            DateTime startTime = DateTime.UtcNow;
            while (Queue.Count != 0)
            {
                if (!IsRunning)
                    break;

                if (cancellationToken.IsCancellationRequested)
                    break;

                cancellationToken.WaitHandle.WaitOne(100);
                if (DateTime.UtcNow - startTime > waitTime)
                    break;
            }
            return Queue.Count == 0;
        }

        #endregion

        private void AddLineToQueue(String line, int limit)
        {
            if (limit == 0)
            {
                WriteDebugMessagesFormat("Message longer than {0}", RECURSION_LIMIT * LOG_LENGTH_LIMIT);
                return;
            }

            WriteDebugMessagesFormat("Adding Line: {0}", line);
            if (!IsRunning)
            {
                // We need to load user credentials only
                // if the configuration does not state that DataHub is used;
                // credentials needed only if logs are sent to LE service directly.
                bool credentialsLoaded = false;
                if (!m_UseDataHub)
                {
                    credentialsLoaded = LoadCredentials();
                }

                // If in DataHub mode credentials are ignored.
                if (credentialsLoaded || m_UseDataHub)
                {
                    WriteDebugMessages("Starting Logentries asynchronous socket client.");
                    WorkerThread.Name = "Logentries Log Appender";
                    WorkerThread.IsBackground = true;
                    WorkerThread.Start();
                    IsRunning = true;
                }
            }

            WriteDebugMessagesFormat("Queueing: {0}", line);

            String trimmedEvent = line.TrimEnd(TrimChars);

            if (trimmedEvent.Length > LOG_LENGTH_LIMIT)
            {
                if (!Queue.TryAdd(trimmedEvent.Substring(0, LOG_LENGTH_LIMIT)))
                {
                    WriteDebugMessages(QueueOverflowMessage);
                    Queue.Take();
                    Queue.TryAdd(trimmedEvent);
                }

                AddLineToQueue(trimmedEvent.Substring(LOG_LENGTH_LIMIT, trimmedEvent.Length), limit - 1);
            }
            else
            {
                // Try to append data to queue.
                if (!Queue.TryAdd(trimmedEvent))
                {
                    WriteDebugMessages(QueueOverflowMessage);
                    Queue.Take();
                    Queue.TryAdd(trimmedEvent);
                }
            }
        }
    }
}
