using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace LogentriesDotNet
{
	/// <summary>
	/// Inspired by https://github.com/mattwcole/gelf-extensions-logging/blob/dev/src/Gelf.Extensions.Logging/LoggingBuilderExtensions.cs
	/// </summary>
	public static class LoggingBuilderExtensions
	{
		public static ILoggingBuilder AddLogentries(this ILoggingBuilder builder)
		{
			builder.AddConfiguration();
			builder.Services.AddSingleton<ILoggerProvider, LogentriesLoggerProvider>();
			builder.Services.TryAddSingleton<IConfigureOptions<LogentriesLoggerOptions>, LogentriesLoggerOptionsSetup>();
			return builder;
		}

		public static ILoggingBuilder AddLogentries(this ILoggingBuilder builder, Action<LogentriesLoggerOptions> configure)
		{
			if (configure == null)
			{
				throw new ArgumentNullException(nameof(configure));
			}

			builder.AddLogentries();
			builder.Services.Configure(configure);
			return builder;
		}
	}
}
