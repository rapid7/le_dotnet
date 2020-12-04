using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LogentriesDotNet
{
	public class LogentriesLoggerOptionsSetup : ConfigureFromConfigurationOptions<LogentriesLoggerOptions>
	{
		public LogentriesLoggerOptionsSetup(ILoggerProviderConfiguration<LogentriesLoggerProvider> providerConfiguration) : base(providerConfiguration.Configuration)
		{
		}
	}
}
