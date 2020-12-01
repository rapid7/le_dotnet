using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LogentriesDotNet
{
	public class LogentriesLoggerOptionsSetup : ConfigureFromConfigurationOptions<LogentriesLoggerOptions>
	{
		public LogentriesLoggerOptionsSetup(IConfiguration config) : base(config)
		{
		}
	}
}