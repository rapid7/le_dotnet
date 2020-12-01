using System;
using System.Collections.Generic;
using System.Threading;

namespace LogentriesDotNet
{
	/// <summary>
	/// Taken from https://github.com/mattwcole/gelf-extensions-logging/blob/dev/src/Gelf.Extensions.Logging/GelfLogScope.cs
	/// </summary>
	public class LogentriesLogScope
	{
		private LogentriesLogScope(IEnumerable<KeyValuePair<string, object>> additionalFields)
		{
			AdditionalFields = additionalFields;
		}

		public LogentriesLogScope Parent { get; private set; }

		public IEnumerable<KeyValuePair<string, object>> AdditionalFields { get; }

		private static readonly AsyncLocal<LogentriesLogScope> Value = new AsyncLocal<LogentriesLogScope>();

		public static LogentriesLogScope Current
		{
			get => Value.Value;
			set => Value.Value = value;
		}

		public static IDisposable Push(IEnumerable<KeyValuePair<string, object>> additionalFields)
		{
			var parent = Current;
			Current = new LogentriesLogScope(additionalFields) { Parent = parent };

			return new DisposableScope();
		}

		private class DisposableScope : IDisposable
		{
			public void Dispose()
			{
				Current = Current?.Parent;
			}
		}
	}
}