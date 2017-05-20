namespace LogentriesCore.Net
{
    /// <summary>
    /// Logentries Asynchronous Logger configuration parameters
    /// </summary>
    public interface IAsyncLoggerConfig
    {
        /// <summary>
        /// GUID for Token-based input (LOGENTRIES_TOKEN), recommended logging method
        /// </summary>
        string Token { get; set; }

        /// <summary>
        /// Logentries Account Key (LOGENTRIES_ACCOUNT_KEY), for older HTTP PUT logging
        /// </summary>
        string AccountKey { get; set; }

        /// <summary>
        /// Sets the LOCATION value (LOGENTRIES_LOCATION), for older HTTP PUT logging
        /// </summary>
        string Location { get; set; }

        /// <summary>
        ///  Sets the debug flag. Will print error messages to System.Diagnostics.Trace
        /// </summary>
        bool Debug { get; set; }

        /// <summary>
        /// Set to true to use older HTTP PUT logging.
        /// </summary>
        bool UseHttpPut { get; set; }

        /// <summary>
        /// Set to true to use SSL (Token-based or HTTP PUT Logging)
        /// </summary>
        bool UseSsl { get; set; }

        /// <summary>
        /// Set to true to use custom DataHub instance instead of Logentries service.
        /// </summary>
        bool IsUsingDataHub { get; set; }

        /// <summary>
        /// DataHub server address
        /// </summary>
        string DataHubAddress { get; set; }

        /// <summary>
        /// DataHub server port
        /// </summary>
        int DataHubPort { get; set; }

        /// <summary>
        /// Set to true to send HostName alongside with the log message
        /// </summary>
        bool LogHostname { get; set; }

        /// <summary>
        /// User-defined host name. If empty the library will try to obtain it automatically
        /// </summary>
        string HostName { get; set; }

        /// <summary>
        /// Log ID
        /// </summary>
        string LogID { get; set; }
    }
}
