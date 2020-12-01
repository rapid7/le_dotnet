using System;
using LogentriesCore.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogentriesDotNet
{
	public class LogentriesLoggerProvider : ILoggerProvider
	{
		public LogentriesLoggerProvider(IOptions<LogentriesLoggerOptions> options) : this(options.Value)
		{
		}

		public LogentriesLoggerProvider(LogentriesLoggerOptions options)
		{
			if (string.IsNullOrEmpty(options.Token))
			{
				throw new ArgumentException("Logentries token is required.", nameof(options));
			}

			if (string.IsNullOrEmpty(options.Template))
			{
				throw new ArgumentException("Output template is required.", nameof(options));
			}

			this.options = options;
			client = new AsyncLogger();
			client.setToken(options.Token);

			if (options.HostName != null)
				client.setHostName(options.HostName);

			if (options.AccountKey != null)
				client.setAccountKey(options.AccountKey);

			if (options.DataHubAddr != null)
				client.setDataHubAddr(options.DataHubAddr);

			if (options.DataHubPort != null)
				client.setDataHubPort(options.DataHubPort.Value);

			if (options.Debug != null)
				client.setDebug(options.Debug.Value);

			if (options.ImmediateFlush != null)
				client.setImmediateFlush(options.ImmediateFlush.Value);

			if (options.IsUsingDataHub != null)
				client.setIsUsingDataHub(options.IsUsingDataHub.Value);

			if (options.Location != null)
				client.setLocation(options.Location);

			if (options.LogID != null)
				client.setLogID(options.LogID);

			if (options.UseHostName != null)
				client.setUseHostName(options.UseHostName.Value);

			if (options.UseSsl != null)
				client.setUseSsl(options.UseSsl.Value);
		}

		public void Dispose()
		{
		}

		public ILogger CreateLogger(string categoryName)
		{
			return new LogentriesLogger(client, options, categoryName);
		}

		private readonly AsyncLogger client;
		private readonly LogentriesLoggerOptions options;
	}
}