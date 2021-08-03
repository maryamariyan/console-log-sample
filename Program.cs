using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Console.ExampleFormatters.Custom;

namespace Demo
{
    public static class ConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddCustomFormatter(this ILoggingBuilder builder, Action<CustomColorOptions> configure)
        {
            return builder.AddConsole(o => { o.FormatterName = "customName"; })
                .AddConsoleFormatter<CustomColorFormatter, CustomColorOptions>(configure);
        }
    }

    partial class Program
    {
        private const string MessageTemplate_0_Args = "message template #0: Name Batman is 82 years old from Gotham moved here 2 years ago bought 20 donuts";
        private const string MessageTemplate_5_Args = "message template #0: Name {Name} is {Age} years old from {City} moved here {YearsSince} years ago bought {numDonuts} donuts";

        private static string Name = "Batman";
        private static int Age = 82;
        private static string City = "Gotham";
        private static int YearsSince = 2;
        private static long NumDonuts = 20;
        private static int NumLoggerProviders = 7;
        private static int NumExtraLoggers = 0;

        [LoggerMessage(EventId = 1023, Level = LogLevel.Critical, Message = MessageTemplate_0_Args)]
        public static partial void LogCritical_0args_Generated(ILogger logger);

        public static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddCustomFormatter(o =>
                    {
                        o.CustomPrefix = " --------------- ";
                        o.IncludeScopes = true;
                        o.TimestampFormat = "hh:mm:ss ";
                        o.ColorBehavior = LoggerColorBehavior.Default;
                    }).AddSimpleConsole());
            
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Random log \x1B[42mwith green background\x1B[49m message");
            using (logger.BeginScope("[scope is enabled]"))
            {
                logger.LogInformation("Hello World!");
                logger.LogInformation("Logs contain timestamp and log level.");
                logger.LogInformation("Each log message is fit in a single line.");
                LogCritical_0args_Generated(logger);
                logger.LogWarning(MessageTemplate_5_Args, Name, Age, City, YearsSince, NumDonuts);
            }
        }
    }
}

namespace Console.ExampleFormatters.Custom
{
    public class CustomColorOptions : SimpleConsoleFormatterOptions
    {
        public string CustomPrefix { get; set; }
    }
 
    public static class TextWriterExtensions
    {
        const string DefaultForegroundColor = "\x1B[39m\x1B[22m";
        const string DefaultBackgroundColor = "\x1B[49m";

        public static void WriteWithColor(
            this TextWriter textWriter,
            string message,
            ConsoleColor? background,
            ConsoleColor? foreground)
        {
            // Order:
            //   1. background color
            //   2. foreground color
            //   3. message
            //   4. reset foreground color
            //   5. reset background color

            var backgroundColor = background.HasValue ? GetBackgroundColorEscapeCode(background.Value) : null;
            var foregroundColor = foreground.HasValue ? GetForegroundColorEscapeCode(foreground.Value) : null;

            if (backgroundColor != null)
            {
                textWriter.Write(backgroundColor);
            }
            if (foregroundColor != null)
            {
                textWriter.Write(foregroundColor);
            }

            textWriter.WriteLine(message);

            if (foregroundColor != null)
            {
                textWriter.Write(DefaultForegroundColor);
            }
            if (backgroundColor != null)
            {
                textWriter.Write(DefaultBackgroundColor);
            }
        }

        static string GetForegroundColorEscapeCode(ConsoleColor color) =>
            color switch
            {
                ConsoleColor.Black =>       "\x1B[30m",
                ConsoleColor.DarkRed =>     "\x1B[31m",
                ConsoleColor.DarkGreen =>   "\x1B[32m",
                ConsoleColor.DarkYellow =>  "\x1B[33m",
                ConsoleColor.DarkBlue =>    "\x1B[34m",
                ConsoleColor.DarkMagenta => "\x1B[35m",
                ConsoleColor.DarkCyan =>    "\x1B[36m",
                ConsoleColor.Gray =>        "\x1B[37m",
                ConsoleColor.Red =>         "\x1B[1m\x1B[31m",
                ConsoleColor.Green =>       "\x1B[1m\x1B[32m",
                ConsoleColor.Yellow =>      "\x1B[1m\x1B[33m",
                ConsoleColor.Blue =>        "\x1B[1m\x1B[34m",
                ConsoleColor.Magenta =>     "\x1B[1m\x1B[35m",
                ConsoleColor.Cyan =>        "\x1B[1m\x1B[36m",
                ConsoleColor.White =>       "\x1B[1m\x1B[37m",

                _ => DefaultForegroundColor
            };

        static string GetBackgroundColorEscapeCode(ConsoleColor color) =>
            color switch
            {
                ConsoleColor.Black =>       "\x1B[40m",
                ConsoleColor.DarkRed =>     "\x1B[41m",
                ConsoleColor.DarkGreen =>   "\x1B[42m",
                ConsoleColor.DarkYellow =>  "\x1B[43m",
                ConsoleColor.DarkBlue =>    "\x1B[44m",
                ConsoleColor.DarkMagenta => "\x1B[45m",
                ConsoleColor.DarkCyan =>    "\x1B[46m",
                ConsoleColor.Gray =>        "\x1B[47m",

                _ => DefaultBackgroundColor
            };
    }

    public sealed class CustomColorFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable _optionsReloadToken;
        private CustomColorOptions _formatterOptions;

        private bool ConsoleColorFormattingEnabled =>
            _formatterOptions.ColorBehavior == LoggerColorBehavior.Enabled ||
            _formatterOptions.ColorBehavior == LoggerColorBehavior.Default &&
            System.Console.IsOutputRedirected == false;

        public CustomColorFormatter(IOptionsMonitor<CustomColorOptions> options)
            // Case insensitive
            : base("customName") =>
            (_optionsReloadToken, _formatterOptions) =
                (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(CustomColorOptions options) =>
            _formatterOptions = options;

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            if (logEntry.Exception is null)
            {
                // return;
            }

            string message =
                logEntry.Formatter(
                    logEntry.State, logEntry.Exception);

            if (message == null)
            {
                return;
            }

            CustomLogicGoesHere(textWriter);
            textWriter.WriteLine(message);
        }

        private void CustomLogicGoesHere(TextWriter textWriter)
        {
            if (ConsoleColorFormattingEnabled)
            {
                textWriter.WriteWithColor(
                    _formatterOptions.CustomPrefix,
                    ConsoleColor.Black,
                    ConsoleColor.Green);
            }
            else
            {
                textWriter.WriteLine(_formatterOptions.CustomPrefix);
            }
        }

        public void Dispose() => _optionsReloadToken?.Dispose();
    }
}