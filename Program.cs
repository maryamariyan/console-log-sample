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
        private const string MessageTemplate_1_Arg = "log #{LogNumber}";
        private const string MessageTemplate_N_Arg = "log #{F1} #{F2} #{F3} #{F4} #{F5} #{F6} #{F7} #{F8} #{F9} #{F10} #{F11} #{F12} #{F13}";

        [LoggerMessage(EventId = 1023, Level = LogLevel.Critical, Message = MessageTemplate_1_Arg)]
        public static partial void LogCritical_1_Arg_Generated(ILogger logger, long logNumber);

        [LoggerMessage(EventId = 1024, Message = MessageTemplate_N_Arg)]
        public static partial void Log_N_Arg_Generated(ILogger logger,
            LogLevel level,
            long f1,
            long f2,
            long f3,
            long f4,
            long f5,
            long f6,
            long f7,
            long f8,
            long f9,
            long f10,
            long f11,
            long f12,
            long f13);

        public static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddCustomFormatter(o =>
                    {
                        o.CustomPrefix = " >>> ";
                        o.ColorBehavior = LoggerColorBehavior.Default;
                    }));
            
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            int c = 0;
            Log_N_Arg_Generated(logger, LogLevel.Critical, 
                c++, c++, c++, 
                c++, c++, c++, 
                c++, c++, c++, 
                c++, c++, c++, c++);
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

            textWriter.Write(message);

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
            textWriter.Write(message);
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
                textWriter.Write(_formatterOptions.CustomPrefix);
            }
        }

        public void Dispose() => _optionsReloadToken?.Dispose();
    }
}