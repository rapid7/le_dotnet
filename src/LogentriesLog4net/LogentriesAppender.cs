using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Appender;
using log4net.Core;

using LogentriesCore.Net;

namespace log4net.Appender
{
    class LogentriesAppender : AppenderSkeleton
    {
        private AsyncLogger logentriesAsync;

        public LogentriesAppender()
        {
            logentriesAsync = new AsyncLogger();
        }

        #region attributeMethods

        /* Option to set LOGENTRIES_TOKEN programmatically or in appender definition. */
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

        /* Option to set LOGENTRIES_ACCOUNT_KEY programmatically or in appender definition. */
        public String AccountKey
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

        /* Option to set LOGENTRIES_LOCATION programmatically or in appender definition. */
        public String Location
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

        /* Set to true to always flush the TCP stream after every written entry. */
        public bool ImmediateFlush
        {
            get
            {
                return logentriesAsync.getImmediateFlush();
            }
            set
            {
                logentriesAsync.setImmediateFlush(value);
            }
        }

        /* Debug flag. */
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


        /* Set to true to use HTTP PUT logging. */
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

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseHttpPut property instead.")]
        public bool HttpPut
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


        /* Set to true to use SSL with HTTP PUT logging. */
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

        /* This property exists for backward compatibility with older configuration XML. */
        [Obsolete("Use the UseHttpPut property instead.")]
        public bool Ssl
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

        #endregion

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

        protected override void OnClose()
        {
            logentriesAsync.interruptWorker();
        }
    }
}
