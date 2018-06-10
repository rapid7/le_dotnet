using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

using LogentriesCore.Net;

namespace NLog.Targets
{
    [Target("Logentries")]
    public sealed class LogentriesTarget : TargetWithLayout, IAsyncLoggerConfig
    {
        class NLogAsyncLogger : AsyncLogger
        {
            protected override void WriteDebugMessages(string message, Exception ex)
            {
                base.WriteDebugMessages(message, ex);
                InternalLogger.Warn(string.Concat(message, " Exception: ", ex.ToString()));
            }

            public override bool LoadCredentials()
            {
                bool success = base.LoadCredentials();
                if (!success)
                {
                    if (getUseHttpPut())
                    {
                        InternalLogger.Warn("Failed to load credentials for LogEntries (PUT), please check LOGENTRIES_ACCOUNT_KEY or LOGENTRIES_LOCATION configuration");
                    }
                    else
                    {
                        InternalLogger.Warn("Failed to load credentials for LogEntries (GET), please check LOGENTRIES_TOKEN configuration");
                    }
                }
                return success;
            }
        }

        private NLogAsyncLogger logentriesAsync;

        public LogentriesTarget()
        {
            logentriesAsync = new NLogAsyncLogger();
        }

        /// <inheritdoc />
        public bool Debug 
        {
            get { return logentriesAsync.getDebug(); }
            set { logentriesAsync.setDebug(value); } 
        }

        /// <inheritdoc />
        public bool IsUsingDataHub
        {
            get { return logentriesAsync.getIsUsingDataHab(); }
            set { logentriesAsync.setIsUsingDataHub(value); }
        }

        /// <inheritdoc />
        public string DataHubAddress
        {
            get { return logentriesAsync.getDataHubAddr(); }
            set { logentriesAsync.setDataHubAddr(value); }
        }

        /// <inheritdoc />
        public int DataHubPort
        {
            get { return logentriesAsync.getDataHubPort(); }
            set { logentriesAsync.setDataHubPort(value); }
        }

        /// <inheritdoc />
        public bool UseHttpPut
        {
            get { return logentriesAsync.getUseHttpPut(); }
            set { logentriesAsync.setUseHttpPut(value); }
        }

        /// <inheritdoc />
        public bool UseSsl
        {
            get { return logentriesAsync.getUseSsl(); }
            set { logentriesAsync.setUseSsl(value); }
        }

        /// <inheritdoc />
        public string Token
        {
            get { return logentriesAsync.getToken(); }
            set { logentriesAsync.setToken(value); }
        }

        /// <inheritdoc />
        public string AccountKey
        {
            get { return logentriesAsync.getAccountKey(); }
            set { logentriesAsync.setAccountKey(value); }
        }

        /// <inheritdoc />
        public string Location
        {
            get { return logentriesAsync.getLocation(); }
            set { logentriesAsync.setLocation(value); }
        }

        /// <inheritdoc />
        public bool LogHostname
        {
            get { return logentriesAsync.getUseHostName(); }
            set { logentriesAsync.setUseHostName(value); }
        }

        /// <inheritdoc />
        public string HostName
        {
            get { return logentriesAsync.getHostName(); }
            set { logentriesAsync.setHostName(value); }
        }

        /// <inheritdoc />
        public string LogID
        {
            get { return logentriesAsync.getLogID(); }
            set { logentriesAsync.setLogID(value); }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the AccountKey property instead.")]
        public string Key
        {
            get { return AccountKey; }
            set { AccountKey = value; }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the DataHubAddress property instead.")]
        public string DataHubAddr
        {
            get { return DataHubAddress; }
            set { DataHubAddress = value; }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseHttpPut property instead.")]
        public bool HttpPut
        {
            get { return logentriesAsync.getUseHttpPut(); }
            set { logentriesAsync.setUseHttpPut(value); }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseSsl property instead.")]
        public bool Ssl
        {
            get { return logentriesAsync.getUseSsl(); }
            set { logentriesAsync.setUseSsl(value); }
        }

        [Obsolete("No longer used.")]
        public bool KeepConnection { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            //Render message content
            String renderedEvent = this.Layout.Render(logEvent);
            logentriesAsync.AddLine(renderedEvent);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();
            logentriesAsync.interruptWorker();
        }

        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            if (!logentriesAsync.FlushQueue(TimeSpan.FromMilliseconds(50)))
            {
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        InternalLogger.Trace("Waiting for AsyncLogger queue flush");
                        if (logentriesAsync.FlushQueue(TimeSpan.FromSeconds(5)))
                        {
                            InternalLogger.Trace("Completed AsyncLogger queue flush");
                            asyncContinuation(null);
                            return;
                        }
                    }
                    InternalLogger.Warn("Timeout while waiting for AsyncLogger queue flush");
                    asyncContinuation(new TimeoutException("AsyncLogger queues are not empty"));
                }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskCreationOptions.None, System.Threading.Tasks.TaskScheduler.Default);
            }
            else
            {
                asyncContinuation(null);
            }
        }
    }
}
