using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Appender;
using log4net.Core;

using LogentriesCore.Net;

namespace log4net.Appender
{
    public class LogentriesAppender : AppenderSkeleton, IAsyncLoggerConfig
    {
        class Log4netAsyncLogger : AsyncLogger
        {
            protected override void WriteDebugMessages(string message, Exception ex)
            {
                base.WriteDebugMessages(message, ex);
                log4net.Util.LogLog.Warn(GetType(), message, ex);
            }

            public override bool LoadCredentials()
            {
                bool success = base.LoadCredentials();
                if (!success)
                {
                    if (getUseHttpPut())
                    {
                        log4net.Util.LogLog.Warn(GetType(), "Failed to load credentials for LogEntries (PUT), please check LOGENTRIES_ACCOUNT_KEY or LOGENTRIES_LOCATION configuration");
                    }
                    else
                    {
                        log4net.Util.LogLog.Warn(GetType(), "Failed to load credentials for LogEntries (GET), please check LOGENTRIES_TOKEN configuration");
                    }
                }
                return success;
            }
        }

        private Log4netAsyncLogger logentriesAsync;

        public LogentriesAppender()
        {
            logentriesAsync = new Log4netAsyncLogger();
        }

        /// <inheritdoc />
        public string Token
        {
            get
            {
                return logentriesAsync.getToken();
            }
            set
            {
                logentriesAsync.setToken(value);
            }
        }

        /// <inheritdoc />
        public string AccountKey
        {
            get
            {
                return logentriesAsync.getAccountKey();
            }
            set
            {
                logentriesAsync.setAccountKey(value);
            }
        }

        /// <inheritdoc />
        public string Location
        {
            get
            {
                return logentriesAsync.getLocation();
            }
            set
            {
                logentriesAsync.setLocation(value);
            }
        }

        /// <inheritdoc />
        public bool Debug
        {
            get
            {
                return logentriesAsync.getDebug();
            }
            set
            {
                logentriesAsync.setDebug(value);
            }
        }

        /// <inheritdoc />
        public bool UseHttpPut
        {
            get
            {
                return logentriesAsync.getUseHttpPut();
            }
            set
            {
                logentriesAsync.setUseHttpPut(value);
            }
        }

        /// <inheritdoc />
        public bool UseSsl
        {
            get
            {
                return logentriesAsync.getUseSsl();
            }
            set
            {
                logentriesAsync.setUseSsl(value);
            }
        }

        /// <inheritdoc />
        public bool IsUsingDataHub
        {
            get
            {
                return logentriesAsync.getIsUsingDataHab();
            }
            set
            {
                logentriesAsync.setIsUsingDataHub(value);
            }
        }

        /// <inheritdoc />
        public string DataHubAddress
        {
            get
            {
                return logentriesAsync.getDataHubAddr();
            }
            set
            {
                logentriesAsync.setDataHubAddr(value);
            }
        }

        /// <inheritdoc />
        public int DataHubPort
        {
            get
            {
                return logentriesAsync.getDataHubPort();
            }
            set
            {
                logentriesAsync.setDataHubPort(value);
            }
        }

        /// <inheritdoc />
        public bool LogHostname
        {
            get
            {
                return logentriesAsync.getUseHostName();
            }
            set
            {
                logentriesAsync.setUseHostName(value);
            }
        }

        /// <inheritdoc />
        public String HostName
        {
            get
            {
                return logentriesAsync.getHostName();
            }
            set
            {
                logentriesAsync.setHostName(value);
            }
        }

        /// <inheritdoc />
        public String LogID
        {
            get
            {
                return logentriesAsync.getLogID();
            }
            set
            {
                logentriesAsync.setLogID(value);
            }
        }

        [Obsolete("No longer used. Flush always enabled")]
        public bool ImmediateFlush
        {
            get { return true; } set { }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseHttpPut property instead.")]
        public bool HttpPut
        {
            get
            {
                return UseHttpPut;
            }
            set
            {
                UseHttpPut = value;
            }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseSsl property instead.")]
        public bool Ssl
        {
            get
            {
                return UseSsl;
            }
            set
            {
                UseSsl = value;
            }
        }

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the DataHubAddress property instead.")]
        public String DataHubAddr
        {
            get
            {
                return DataHubAddress;
            }
            set
            {
                DataHubAddress = value;
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var renderedEvent = RenderLoggingEvent(loggingEvent);
            logentriesAsync.AddLine(renderedEvent);
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var logEvent in loggingEvents)
            {
                this.Append(logEvent);
            }
        }

        protected override bool RequiresLayout
        {
            get
            {
                return true;
            }
        }

        public override bool Flush(int millisecondsTimeout)
        {
            return logentriesAsync.FlushQueue(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        protected override void OnClose()
        {
            logentriesAsync.interruptWorker();
        }
    }
}
