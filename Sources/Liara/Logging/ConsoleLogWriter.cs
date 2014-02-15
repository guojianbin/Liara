﻿// Author: Prasanna V. Loganathar
// Project: Liara
// Copyright (c) Launchark Technologies. All rights reserved.
// See License.txt in the project root for license information.
// 
// Created: 8:31 AM 15-02-2014

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Liara.Logging
{
    public sealed class ConsoleLogWriter : ILiaraLogWriter
    {
        public delegate void MessageReceived(object sender, LogMessage args);

        private static readonly IndentedConsoleWriter ConsoleWriter = new IndentedConsoleWriter();
        private readonly object lockObj = new object();

        public ConsoleLogWriter()
        {
            IsEnabled = true;
            SetupListener();
        }

        public int Priority { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>
        ///     Write to log with the default log name.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(string message, params object[] arguments)
        {
            if (!IsEnabled)
                return;
            WriteInternal(GetCallerTypeName(), string.Format(message, arguments));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task WriteAsync(string message, params object[] arguments)
        {
            if (!IsEnabled)
                return;
            var logName = GetCallerTypeName(3);
            await Task.Run(() => WriteInternal(logName, string.Format(message, arguments)));
        }

        /// <summary>
        ///     Write to log with the given log name.
        /// </summary>
        /// <param name="logName">Can be either a string or the object who's type name will be used as the name.</param>
        /// <param name="message">The message string.</param>
        /// <param name="arguments">The message string format arguments.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteTo(string logName, string message, params object[] arguments)
        {
            if (!IsEnabled)
                return;
            WriteInternal(logName, string.Format(message, arguments));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task WriteToAsync(string logName, string message, params object[] arguments)
        {
            if (!IsEnabled)
                return;

            await Task.Run(() => WriteInternal(logName, string.Format(message, arguments)));
        }

        /// <summary>
        ///     Write exception details to log with the default log name.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="throwException">Throws the exception after writing to log.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteException(Exception exception, bool throwException = false)
        {
            if (!IsEnabled)
                return;
            WriteExceptionInternal(GetCallerTypeName(), exception);
            if (throwException)
                throw exception;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task WriteExceptionAsync(Exception exception, bool throwException = false)
        {
            if (!IsEnabled)
                return;
            var logName = GetCallerTypeName(3);
            await Task.Run(() => WriteExceptionInternal(logName, exception));
            if (throwException)
                throw exception;
        }

        /// <summary>
        ///     Write exception details to the log with the given log name.
        /// </summary>
        /// <param name="logName">Can be either a string or the object who's type name will be used as the name.</param>
        /// <param name="exception"></param>
        /// <param name="throwException">Throws the exception after writing to log.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WriteExceptionTo(string logName, Exception exception, bool throwException = false)
        {
            if (!IsEnabled)
                return;
            WriteExceptionInternal(logName, exception);
            if (throwException)
                throw exception;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task WriteExceptionToAsync(string logName, Exception exception, bool throwException = false)
        {
            if (!IsEnabled)
                return;
            await Task.Run(() => WriteExceptionInternal(logName, exception));
            if (throwException)
                throw exception;
        }

        private void SetupListener()
        {
            var messageThrottler = Observable.FromEventPattern<LogMessage>(this, "OnMessageReceived");
            messageThrottler.Buffer(TimeSpan.FromSeconds(1)).Subscribe(events =>
            {
                var count = events.Count;
                if (count > 0)
                {
                    lock (lockObj)
                    {
                        if (count > 1)
                        {
                            Console.WriteLine();
                            WriteDateTime(events.First().EventArgs.TimeStamp, suffix: " : \r\n");
                            Console.WriteLine();

                            var category = events.GroupBy(e => e.EventArgs.LogName);

                            foreach (var items in category)
                            {
                                if (items.Count() > 1)
                                {
                                    WriteMultiLine(items);
                                }
                                else
                                {
                                    var evt = items.First().EventArgs;
                                    WriteSingleLine(evt);
                                }
                            }
                        }
                        else if (count == 1)
                        {
                            var evt = events.First().EventArgs;
                            WriteSingleLine(evt);
                        }
                    }
                }
            });
        }

        public event MessageReceived OnMessageReceived;

        private void WriteSingleLine(LogMessage message)
        {
            WriteDateTime(message.TimeStamp);
            Console.WriteLine(" - {0}: {1}", message.LogName, message.Message);
        }

        private void WriteMultiLine(IGrouping<string, EventPattern<LogMessage>> items)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(items.Key + ":");
            Console.ForegroundColor = color;
            Console.WriteLine();
            foreach (var message in items)
            {
                if (!string.IsNullOrWhiteSpace(message.EventArgs.Message))
                    Console.WriteLine(message.EventArgs.Message);
            }
        }

        private void WriteDateTime(DateTime value, string prefix = null, string suffix = null, string stringFormat = "s")
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(prefix + value.ToString(stringFormat) + suffix);
            Console.ForegroundColor = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetCallerTypeName(int skipFrames = 1)
        {
            var framesToSkip = skipFrames;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                framesToSkip++;
            }
            var stackFrame = new StackFrame(framesToSkip);
// ReSharper disable once PossibleNullReferenceException
            return stackFrame.GetMethod().DeclaringType.FullName;
        }

        private void WriteExceptionInternal(string logName, Exception exception)
        {
            lock (lockObj)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                WriteExceptionHandler(logName, exception);
                Console.WriteLine("End of exception.");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        private void WriteCurrentDateTime()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(DateTime.Now.ToString("s"));
            Console.ForegroundColor = color;
        }

        private void WriteExceptionHandler(string logName, Exception exception)
        {
            WriteCurrentDateTime();
            Console.WriteLine(" - {0} - Exception:", logName);
            ConsoleWriter.Indent++;

            if (exception.GetType() == typeof (AggregateException))
            {
                Console.WriteLine(" ExceptionType: {0}", exception.GetType().Name);
                Console.WriteLine();
                Console.WriteLine(" Inner Exceptions:");
                Console.WriteLine();
                ConsoleWriter.Indent++;
                foreach (var ex in ((AggregateException) exception).InnerExceptions)
                {
                    WriteExceptionHandler(logName, ex);
                }
                ConsoleWriter.Indent--;
            }

            PrintExceptionExplanation(exception);
            ConsoleWriter.Indent--;
        }

        private void PrintExceptionExplanation(Exception exception)
        {
            if (exception.GetType() != typeof (AggregateException))
            {
                Console.WriteLine("ExceptionType: {0}", exception.GetType().Name);
                Console.WriteLine("Message: {0}", exception.Message);
                Console.WriteLine("Source: {0}", exception.Source);
                Console.WriteLine("TargetSite: {0}", exception.TargetSite);
                Console.WriteLine("StackTrace: ");
                Console.WriteLine(exception.StackTrace);
            }
        }

        private void WriteInternal(string logName, string message)
        {
            OnMessageReceived(this, new LogMessage {LogName = logName, Message = message, TimeStamp = DateTime.Now});
        }

        private class IndentedConsoleWriter : TextWriter
        {
            private readonly TextWriter sysConsole;
            private bool doIndent;

            public IndentedConsoleWriter()
            {
                sysConsole = Console.Out;
                Console.SetOut(this);
            }

            public int Indent { get; set; }

            public override System.Text.Encoding Encoding
            {
                get { return sysConsole.Encoding; }
            }

            public override void Write(char ch)
            {
                if (doIndent)
                {
                    doIndent = false;
                    for (int ix = 0; ix < Indent; ++ix) sysConsole.Write("  ");
                }
                sysConsole.Write(ch);
                if (ch == '\n') doIndent = true;
            }
        }

        public class LogMessage
        {
            public DateTime TimeStamp { get; set; }
            public string LogName { get; set; }
            public string Message { get; set; }
        }
    }
}