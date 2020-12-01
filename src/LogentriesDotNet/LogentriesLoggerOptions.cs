using Microsoft.Extensions.Logging;

namespace LogentriesDotNet
{
	public class LogentriesLoggerOptions
	{
		public string Token { get; set; }
		
		public string Template { get; set; }
		public string HostName { get; set; }
		public string AccountKey { get; set; }
		public string DataHubAddr { get; set; }
		public int? DataHubPort { get; set; }
		public bool? Debug { get; }
		public bool? ImmediateFlush { get; set; }
		public bool? IsUsingDataHub { get; set; }
		public string Location { get; set; }
		public string LogID { get; set; }
		public bool? UseHostName { get; set; }
		public bool? UseSsl { get; set; }
		public LogLevel Level { get; set; }
		public bool IncludeScopes { get; set; }
	}
}