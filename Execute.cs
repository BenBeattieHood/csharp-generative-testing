using PageUp.Functional.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerativePropertyTesting
{
    public static class Execute
    {
        private static IEnumerable<string> LogValues(System.Collections.IEnumerable values)
        {
            foreach (var value in values)
            {
                yield return LogValue(value);
            }
        }

        public static string LogValue<T>(T value) => 
            EqualityComparer<T>.Default.Equals(value, default(T)) ? $"[default()]" 
            : value is System.Collections.IEnumerable values ? $"[{string.Join(", ", LogValues(values))}]"
            : $"{value}";

        public static void LogTo<TData, TResult, TError>(TestResult<TData, TResult, TError> result, System.IO.TextWriter writer)
        {
            if (result is TestResult<TData, TResult, TError>.Success success)
            {
                writer.WriteLine($"SUCCESS: {result.Name}: {result.Data}: {success.Value}");
            }
            else if (result is TestResult<TData, TResult, TError>.Failure failure)
            {
                writer.WriteLine($"FAILURE: {result.Name}: {result.Data}: {failure.Value}");
            }
        }

        public static void LogTo<TData, TResult, TError>(TestResult<TData, Result<TResult, Exception>, TError> result, System.IO.TextWriter writer)
        {
            if (result is TestResult<TData, Result<TResult, Exception>, TError>.Success success)
            {
                writer.WriteLine($"SUCCESS: {result.Name}: {result.Data}: {success.Value.Match(ok: value => $"✓: {ReflectionUtils.GetTypeName(typeof(TResult))}{LogValue(value)}", error: error => $"✗: {ReflectionUtils.GetTypeName(error == null ? typeof(Exception) : error.GetType())}{LogValue(error)}")}");
            }
            else if (result is TestResult<TData, Result<TResult, Exception>, TError>.Failure failure)
            {
                writer.WriteLine($"FAILURE: {result.Name}: {result.Data}: {failure.Value}");
            }
        }

        //public static void Run<TData, TResult, TError>(TestResult<TData, Result<TResult, TError>, Exception> testResult) =>
        //    Run(new [] { testResult });
        
        public static void Run<TData, TResult>(IEnumerable<TestResult<TData, TResult, Exception>> testResults) =>
            Run(testResults, (TestResult<TData, TResult, Exception> result) => {});

        //public static void Run<TData, TResult, TError>(TestResult<TData, Result<TResult, TError>, Exception> testResult, Action<TData, bool> log) =>
        //    Run(new [] { testResult }, log);

        public static void Run<TData, TResult>(IEnumerable<TestResult<TData, TResult, Exception>> testResults, Action<TData, bool> log)
        => Run(
            testResults, 
            result =>
            {
                if (result is TestResult<TData, TResult, Exception>.Success success)
                {
                    log(success.Data, true);
                }
                else if (result is TestResult<TData, TResult, Exception>.Failure failure)
                {
                    log(failure.Data, false);
                }
            }
        );

        public static void Run<TData, TResult>(IEnumerable<TestResult<TData, TResult, Exception>> testResults, System.IO.TextWriter logTo) 
        =>
        Run(testResults, result => LogTo(result, logTo));

        public static void Run<TData, TResult>(IEnumerable<TestResult<TData, Result<TResult, Exception>, Exception>> testResults, System.IO.TextWriter logTo) 
        =>
        Run(testResults, result => LogTo(result, logTo));

        public static void Run<TData, TResult>(IEnumerable<TestResult<TData, TResult, Exception>> testResults, Action<TestResult<TData, TResult, Exception>> log)
        {
            if (testResults == null)
            {
                throw new ArgumentNullException(nameof(testResults));
            }

            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var exceptions = new Lazy<List<Exception>>(() => new List<Exception>());
            foreach (var testResult in testResults)
            {
                log(testResult);
                if (testResult is TestResult<TData, TResult, Exception>.Failure failure) 
                {
                    exceptions.Value.Add(failure.Value);
                }
            }
            if (exceptions.IsValueCreated)
            {
                throw new AggregateException(exceptions.Value);
            }
        }
    }
}
