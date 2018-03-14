# Logging To Logentries.

Logentries currently has plugins for NLog, Log4net and Serilog.

To configure your application to log to Logentries, you will need to perform the following tasks:

1. Create a Logentries account.
2. Create a host and log to receive your log data.
3. Adding the Plugin library and the appropriate Logentries Appender libraries to your application.
4. Configure the Plugin and the Logentries appender in your application.
5. Send log messages from your application.

These steps are outlined in further detail below.

## Creating Your Logentries Account

You can register your account on Logentries by browning to [https://logentries.com](https://logentries.com) and simply clicking `Sign Up` at the top of the page.

## Creating the Host and Log

Once logged in to your Logentries account, create a new host with a name that best represents your application. Select this host and create a new log with a source type of `Token TCP` (see below for more information) and a name that represents what you will be logging.

Please note that Logentries reads no particular meaning into the names of your hosts and logs; they are primarily for your own benefit in keeping your logs organized.

## Links to Documentation

## NLog

Visit [NLog docs](https://docs.logentries.com/docs/nlog)

## Log4Net

Visit [Log4Net](https://docs.logentries.com/docs/log4net)

## Serilog

Visit [Serilog](https://docs.logentries.com/docs/net-serilog)

## Contact Support
Please email our support team at support@logentries.com if you need any help.
