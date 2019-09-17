using System;
using System.Collections.Generic;
using System.Text;
using static PageUp.Functional.Core.Prelude;
using PageUp.Functional.Core;
using Void = PageUp.Functional.Core.Void;

namespace GenerativePropertyTesting
{
    public abstract class TestResult<TData, TSuccess, TFailure>
    {
        public static (
            Func<string, TData, TSuccess, TestResult<TData, TSuccess, TFailure>> Success,
            Func<string, TData, TFailure, TestResult<TData, TSuccess, TFailure>> Failure
        ) GetBaseConstructors() =>
        (
            Success: (name, data, value) => new Success(name, data, value),
            Failure: (name, data, value) => new Failure(name, data, value)
        );
        
        public readonly string Name;
        public readonly TData Data;
        protected TestResult(string name, TData data)
        {
            Name = name;
            Data = data;
        }
        public sealed class Success : TestResult<TData, TSuccess, TFailure>
        {
            public readonly TSuccess Value;
            public Success(string name, TData data, TSuccess value) : base(name, data){ Value = value; }
        }
        public sealed class Failure : TestResult<TData, TSuccess, TFailure>
        {
            public readonly TFailure Value;
            public Failure(string name, TData data, TFailure value) : base(name, data){ Value = value; }
        }
    }
}
