// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Quantum.QsCompiler.CommandLineCompiler;
using Xunit;

namespace Tests.PerformanceTracking
{
    public class CompilationTrackerTests
    {
        private static readonly int ErrorMarginInMs = 25;
        private static readonly string ResultsFolderRootName = "Results";

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(825)]
        [InlineData(1000)]
        [InlineData(5050)]
        public void MeasureSingleTask(int durationInMs)
        {
            const string taskName = "TestTask";
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.Start,
                null,
                taskName);

            Thread.Sleep(durationInMs);
            CompilationTracker.OnCompilationTaskEvent(
                Microsoft.Quantum.QsCompiler.Diagnostics.CompilationTaskEventType.End,
                null,
                taskName);

            var resultsFolder = Path.Combine(ResultsFolderRootName, GetCurrentMethodName());
            CompilationTracker.PublishResults(resultsFolder, true);
            var resultsFile = Path.Combine(resultsFolder, CompilationTracker.CompilationPerfDataFileName);
            var resultsDictionary = ParseJsonToDictionary(resultsFile);
            Assert.True(resultsDictionary.TryGetValue(taskName, out var measuredDurationInMs));
            var (lowerLimit, upperLimit) = CalculateMeasurementLimits(durationInMs, ErrorMarginInMs);
            Assert.InRange(measuredDurationInMs, lowerLimit, upperLimit);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethodName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1);
            return frame.GetMethod().Name;
        }

        private static (int, int) CalculateMeasurementLimits(int expected, int errorMargin)
        {
            return (Math.Max(expected - errorMargin, 0), expected + errorMargin);
        }

        private static IDictionary<string, int> ParseJsonToDictionary(string path)
        {
            var jsonString = File.ReadAllText(path);
            var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString);
            return dictionary;
        }
    }
}
