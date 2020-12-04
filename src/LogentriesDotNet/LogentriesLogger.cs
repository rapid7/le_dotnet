using System;
using System.Collections.Generic;
using System.Linq;
using LogentriesCore.Net;
using Microsoft.Extensions.Logging;

namespace LogentriesDotNet
{
	/// <summary>
	/// Based on https://github.com/mattwcole/gelf-extensions-logging/blob/dev/src/Gelf.Extensions.Logging/GelfLogger.cs
	/// </summary>
	public class LogentriesLogger : ILogger
	{
		public LogentriesLogger(AsyncLogger client, LogentriesLoggerOptions options, string category)
		{
			this.category = category;
			this.client = client;
			this.options = options;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			var message = formatter(state, exception);

			client.AddLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {category} - [{logLevel}] {message}{Environment.NewLine}{exception}");
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel >= options.Level;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			switch(state)
			{
				case IEnumerable<KeyValuePair<string, object>> fields: return LogentriesLogScope.Push(fields);
				case ValueTuple<string, string> field: return BeginValueTupleScope(field);
				case ValueTuple<string, sbyte> field: return BeginValueTupleScope(field);
				case ValueTuple<string, byte> field: return BeginValueTupleScope(field);
				case ValueTuple<string, short> field: return BeginValueTupleScope(field);
				case ValueTuple<string, ushort> field: return BeginValueTupleScope(field);
				case ValueTuple<string, int> field: return BeginValueTupleScope(field);
				case ValueTuple<string, uint> field: return BeginValueTupleScope(field);
				case ValueTuple<string, long> field: return BeginValueTupleScope(field);
				case ValueTuple<string, ulong> field: return BeginValueTupleScope(field);
				case ValueTuple<string, float> field: return BeginValueTupleScope(field);
				case ValueTuple<string, double> field: return BeginValueTupleScope(field);
				case ValueTuple<string, decimal> field: return BeginValueTupleScope(field);
				case ValueTuple<string, object> field: return BeginValueTupleScope(field);
				default: return new NoopDisposable();
			}

			IDisposable BeginValueTupleScope((string, object) field)
			{
				return LogentriesLogScope.Push(new[]
				{
					new KeyValuePair<string, object>(field.Item1, field.Item2)
				});
			}
		}

		private class NoopDisposable : IDisposable
		{
			public void Dispose()
			{
			}
		}

    private static IEnumerable<KeyValuePair<string, object>> GetStateAdditionalFields<TState>(TState state)
    {
      return state is IEnumerable<KeyValuePair<string, object>> logValues
          ? logValues
          : Enumerable.Empty<KeyValuePair<string, object>>();
    }

    private IEnumerable<KeyValuePair<string, object>> GetScopeAdditionalFields()
    {
      var additionalFields = Enumerable.Empty<KeyValuePair<string, object>>();

      if (!options.IncludeScopes)
      {
        return additionalFields;
      }

      var scope = LogentriesLogScope.Current;
      while (scope != null)
      {
        additionalFields = additionalFields.Concat(scope.AdditionalFields);
        scope = scope.Parent;
      }

      return additionalFields.Reverse();
    }

    private static double GetTimestamp()
    {
      var totalMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
      var totalSeconds = totalMilliseconds / 1000d;
      return Math.Round(totalSeconds, 3);
    }

    private readonly LogentriesLoggerOptions options;
    private readonly string category;
		private readonly AsyncLogger client;
	}
}