using System;
using System.Collections.Generic;
using System.Linq;
using static PageUp.Functional.Core.Prelude;
using PageUp.Functional.Core;
using Void = PageUp.Functional.Core.Void;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace GenerativePropertyTesting.Data
{
    public static class TestDataUtils
    {
        public static ((Type, string), Func<IEnumerable<object>>) NamedTypeGenerator<T>(string name, params T[] ts) => ((typeof(T), name), () => ts.Select(x => (object)x));
        public static ((Type, string), Func<IEnumerable<object>>) TypeGenerator<T>(params T[] ts) => ((typeof(T), null), () => ts.Select(x => (object)x));
        
        public static IEnumerable<T> Randomise<T>(IEnumerable<T> values)
        {
            var random = new Random();
            return values.OrderBy(_ => random.Next(int.MaxValue));
        }

        public static IEnumerable<T> RandomOf<T>(int count, IEnumerable<T> items) => Randomise(items).Take(count);
    }

    public class TestData
    {
        public static IEnumerable<((Type, string), Func<IEnumerable<object>>)> StandardPrimitives = new []
        {
            TestDataUtils.TypeGenerator(true, false),
            TestDataUtils.TypeGenerator(byte.MinValue, byte.MaxValue),
            TestDataUtils.TypeGenerator(char.MinValue, char.MaxValue),
            TestDataUtils.TypeGenerator(decimal.MinValue, -1M, 0M, 0.5M, 1M, decimal.MaxValue),
            TestDataUtils.TypeGenerator(double.MinValue, -1D, 0D, 1D, double.MaxValue),
            TestDataUtils.TypeGenerator(float.MinValue, -1F, 0F, 0.5F, 1F, float.MaxValue),
            TestDataUtils.TypeGenerator(int.MinValue, 0, 1, int.MaxValue),
            TestDataUtils.TypeGenerator(long.MinValue, -1L, 0L, 1U, long.MaxValue),
            TestDataUtils.TypeGenerator(sbyte.MinValue, sbyte.MaxValue),
            TestDataUtils.TypeGenerator(short.MinValue, (short)1, short.MaxValue),
            TestDataUtils.TypeGenerator(null, string.Empty, "abc"),
            TestDataUtils.TypeGenerator(uint.MinValue, 1U, uint.MaxValue),
            TestDataUtils.TypeGenerator(ulong.MinValue, 1UL, ulong.MaxValue),
            TestDataUtils.TypeGenerator(ushort.MinValue, (ushort)1, ushort.MaxValue),
            TestDataUtils.TypeGenerator(null, (object)string.Empty)
        };

        public delegate IEnumerable<object> TypeValueFactory(string name);
        public delegate IEnumerable<object> TypeConstructor(Type t, string name, Func<Type, string, IEnumerable<object>> valueFactory);

        private readonly ConcurrentDictionary<(Type, string), Func<IEnumerable<object>>> TypeGenerators;
        private readonly TypeConstructor InternalTypeConstructor;

        //public TestData(
        //    IEnumerable<(Type, IEnumerable<object>)> typeValues,
        //    TypeConstructor typeConstructor = null
        //    )
        //    : this(
        //          typeValues.Select(x => (((Type, string))(x.Item1, null), define(() => x.Item2))),
        //          typeConstructor
        //    ) {}

        private static KeyValuePair<TKey, TValue> KeyValuePair<TKey, TValue>(TKey key, TValue value) => new KeyValuePair<TKey, TValue>(key, value);

        public TestData(
            IEnumerable<((Type, string), Func<IEnumerable<object>>)> typeGenerators,
            TypeConstructor typeConstructor = null
        )
        {
            TypeGenerators = new ConcurrentDictionary<(Type, string), Func<IEnumerable<object>>>(typeGenerators.Select(x => KeyValuePair(x.Item1, x.Item2)));
            InternalTypeConstructor = typeConstructor;
        }
            
        public IEnumerable<TestResult<IEnumerable<object>, Result<object, Exception>, Exception>> Test(
            System.Reflection.MethodInfo method,
            object instance
            )
        => 
        Test(
            method: method,
            instance: instance,
            assert: (data, result) => { }
        );
        
        private IEnumerable<object> GetValuesFor(Type t, string name) 
        {
            if (null != name)
            {
                return
                    TypeGenerators.GetOrAdd(
                        (t, name),
                        () => TypeGenerators.GetOrAdd(
                            (t, null),
                            () => InternalTypeConstructor(t, name, GetValuesFor)
                        )()
                    )();
            }
            else 
            {
                return
                    TypeGenerators.GetOrAdd(
                        (t, null),
                        () => InternalTypeConstructor(t, name, GetValuesFor)
                    )();
            }
        }

        private IEnumerable<T> GetValuesFor<T>(string name)
        => GetValuesFor(typeof(T), name).Select(x => (T)x);

        public IEnumerable<TestResult<IEnumerable<object>, Result<object, Exception>, Exception>> Test(
            System.Reflection.MethodInfo method,
            object instance,
            Action<IEnumerable<object>, Result<object, Exception>> assert
            )
        {
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                var x = new object[0] { };
                return new []
                {
                    Enquire.Test(
                        data: x,
                        title: $"{ReflectionUtils.GetTypeName(method.DeclaringType)}.{method.Name}()",
                        result: Enquire.AsCaught(() => method.Invoke(instance, x)),
                        assert: assert
                    )
                };
            }

            var parameterValuesPermutations = Permutate(
                method.GetParameters().Select(parameter => GetValuesFor(parameter.ParameterType, parameter.Name))
            );
            
            var parameterDefinitions = string.Join(", ", parameters.Select(p => $"{ReflectionUtils.GetTypeName(p.ParameterType)} {p.Name}"));

            return
                from parameterValuesPermutation in parameterValuesPermutations
                let x = parameterValuesPermutation.ToArray()
                select Enquire.Test(
                    data: x,
                    title: $"{ReflectionUtils.GetTypeName(method.DeclaringType)}.{method.Name}({parameterDefinitions})",
                    result: Enquire.AsCaught(() => method.Invoke(instance, x)),
                    assert: assert
                );
        }
        
        public IEnumerable<TestResult<IEnumerable<object>, Result<object, Exception>, Exception>> TestPublicInstanceDeclaredMethods<T>(
            T instance
            )
        => 
        TestMethods<T>(
            instance,
            System.Reflection.BindingFlags.Public 
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.DeclaredOnly
            );

        public IEnumerable<TestResult<IEnumerable<object>, Result<object, Exception>, Exception>> TestMethods<T>(
            T instance,
            System.Reflection.BindingFlags bindingFlags
            )
        {
            foreach (var method in typeof(T).GetMethods(bindingFlags))
            {
                var results = Test(
                    method: method,
                    instance: instance
                    );

                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }

        #region Test<T1>

        private IEnumerable<TestResult<T1, TResult, Exception>> InternalTestMethod<T1, TResult>(
            Func<T1, object[], TestResult<T1, TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = 
                GetValuesFor<T1>(null)
            ;
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation 
                });
        }
        public IEnumerable<TestResult<T1, TResult, Exception>> Test<T1, TResult>(
            Expression<Func<T1, TResult>> method,
            Action<T1, TResult> assert
            )
        =>
        InternalTestMethod<T1, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<T1, Result<TResult, Exception>, Exception>> TestC<T1, TResult>(
            Expression<Func<T1, TResult>> method,
            Action<T1, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2>

        private IEnumerable<TestResult<(T1, T2), TResult, Exception>> InternalTestMethod<T1, T2, TResult>(
            Func<(T1, T2), object[], TestResult<(T1, T2), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2
                });
        }
        public IEnumerable<TestResult<(T1, T2), TResult, Exception>> Test<T1, T2, TResult>(
            Expression<Func<T1, T2, TResult>> method,
            Action<T1, T2, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2), Result<TResult, Exception>, Exception>> TestC<T1, T2, TResult>(
            Expression<Func<T1, T2, TResult>> method,
            Action<T1, T2, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2, T3>

        private IEnumerable<TestResult<(T1, T2, T3), TResult, Exception>> InternalTestMethod<T1, T2, T3, TResult>(
            Func<(T1, T2, T3), object[], TestResult<(T1, T2, T3), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null),
                GetValuesFor<T3>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2,
                    parameterValuesPermutation.Item3
                });
        }
        public IEnumerable<TestResult<(T1, T2, T3), TResult, Exception>> Test<T1, T2, T3, TResult>(
            Expression<Func<T1, T2, T3, TResult>> method,
            Action<T1, T2, T3, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2, T3), Result<TResult, Exception>, Exception>> TestC<T1, T2, T3, TResult>(
            Expression<Func<T1, T2, T3, TResult>> method,
            Action<T1, T2, T3, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2, T3, T4>

        private IEnumerable<TestResult<(T1, T2, T3, T4), TResult, Exception>> InternalTestMethod<T1, T2, T3, T4, TResult>(
            Func<(T1, T2, T3, T4), object[], TestResult<(T1, T2, T3, T4), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null),
                GetValuesFor<T3>(null),
                GetValuesFor<T4>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2,
                    parameterValuesPermutation.Item3,
                    parameterValuesPermutation.Item4
                });
        }
        public IEnumerable<TestResult<(T1, T2, T3, T4), TResult, Exception>> Test<T1, T2, T3, T4, TResult>(
            Expression<Func<T1, T2, T3, T4, TResult>> method,
            Action<T1, T2, T3, T4, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2, T3, T4), Result<TResult, Exception>, Exception>> TestC<T1, T2, T3, T4, TResult>(
            Expression<Func<T1, T2, T3, T4, TResult>> method,
            Action<T1, T2, T3, T4, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2, T3, T4, T5>

        private IEnumerable<TestResult<(T1, T2, T3, T4, T5), TResult, Exception>> InternalTestMethod<T1, T2, T3, T4, T5, TResult>(
            Func<(T1, T2, T3, T4, T5), object[], TestResult<(T1, T2, T3, T4, T5), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null),
                GetValuesFor<T3>(null),
                GetValuesFor<T4>(null),
                GetValuesFor<T5>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2,
                    parameterValuesPermutation.Item3,
                    parameterValuesPermutation.Item4,
                    parameterValuesPermutation.Item5
                });
        }
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5), TResult, Exception>> Test<T1, T2, T3, T4, T5, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, TResult>> method,
            Action<T1, T2, T3, T4, T5, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5), Result<TResult, Exception>, Exception>> TestC<T1, T2, T3, T4, T5, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, TResult>> method,
            Action<T1, T2, T3, T4, T5, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2, T3, T4, T5, T6>

        private IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6), TResult, Exception>> InternalTestMethod<T1, T2, T3, T4, T5, T6, TResult>(
            Func<(T1, T2, T3, T4, T5, T6), object[], TestResult<(T1, T2, T3, T4, T5, T6), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null),
                GetValuesFor<T3>(null),
                GetValuesFor<T4>(null),
                GetValuesFor<T5>(null),
                GetValuesFor<T6>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2,
                    parameterValuesPermutation.Item3,
                    parameterValuesPermutation.Item4,
                    parameterValuesPermutation.Item5,
                    parameterValuesPermutation.Item6
                });
        }
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6), TResult, Exception>> Test<T1, T2, T3, T4, T5, T6, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> method,
            Action<T1, T2, T3, T4, T5, T6, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, T6, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6), Result<TResult, Exception>, Exception>> TestC<T1, T2, T3, T4, T5, T6, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> method,
            Action<T1, T2, T3, T4, T5, T6, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, T6, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion

        #region Test<T1, T2, T3, T4, T5, T6, T7>

        private IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6, T7), TResult, Exception>> InternalTestMethod<T1, T2, T3, T4, T5, T6, T7, TResult>(
            Func<(T1, T2, T3, T4, T5, T6, T7), object[], TestResult<(T1, T2, T3, T4, T5, T6, T7), TResult, Exception>> test
            )
        {
            var parameterValuesPermutations = Permutate(
                GetValuesFor<T1>(null),
                GetValuesFor<T2>(null),
                GetValuesFor<T3>(null),
                GetValuesFor<T4>(null),
                GetValuesFor<T5>(null),
                GetValuesFor<T6>(null),
                GetValuesFor<T7>(null)
            );
                
            return
                from parameterValuesPermutation in parameterValuesPermutations
                select test(parameterValuesPermutation, new object[] { 
                    parameterValuesPermutation.Item1,
                    parameterValuesPermutation.Item2,
                    parameterValuesPermutation.Item3,
                    parameterValuesPermutation.Item4,
                    parameterValuesPermutation.Item5,
                    parameterValuesPermutation.Item6,
                    parameterValuesPermutation.Item7
                });
        }
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6, T7), TResult, Exception>> Test<T1, T2, T3, T4, T5, T6, T7, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> method,
            Action<T1, T2, T3, T4, T5, T6, T7, TResult> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, T6, T7, TResult>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                (TResult)method.Compile().DynamicInvoke(b),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );
        public IEnumerable<TestResult<(T1, T2, T3, T4, T5, T6, T7), Result<TResult, Exception>, Exception>> TestC<T1, T2, T3, T4, T5, T6, T7, TResult>(
            Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> method,
            Action<T1, T2, T3, T4, T5, T6, T7, Result<TResult, Exception>> assert
            )
        =>
        InternalTestMethod<T1, T2, T3, T4, T5, T6, T7, Result<TResult, Exception>>(
            test: (a, b) => Enquire.Test(
                a,
                method.ToString(),
                Enquire.AsCaught(() => (TResult)method.Compile().DynamicInvoke(b)),
                result => assert.DynamicInvoke(b.Append(result).ToArray())
            )
        );

        #endregion
        

        #region Permutate

        private static IEnumerable<IEnumerable<T>> Permutate<T>(params IEnumerable<T>[] source) => Permutate((IEnumerable<IEnumerable<T>>)source);
        private static IEnumerable<IEnumerable<T>> Permutate<T>(IEnumerable<IEnumerable<T>> source)
        {
            switch (source.Count())
            {
                case 0:
                    return source;

                case 1:
                    return 
                        source
                        .First()
                        .Select(x => new [] { x });
                    
                default:
                    return 
                        source
                        .First()
                        .SelectMany(
                            x => Permutate(source.Skip(1)), 
                            (x, result) => result.Prepend(x)
                        );
            }
        }
        private static IEnumerable<(T1, T2)> Permutate<T1, T2>(
            IEnumerable<T1> a, 
            IEnumerable<T2> b
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1]
            );
        });
        private static IEnumerable<(T1, T2, T3)> Permutate<T1, T2, T3>(
            IEnumerable<T1> a, 
            IEnumerable<T2> b, 
            IEnumerable<T3> c
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
            c.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1],
                (T3)x[2]
            );
        });
        private static IEnumerable<(T1, T2, T3, T4)> Permutate<T1, T2, T3, T4>(
            IEnumerable<T1> a, 
            IEnumerable<T2> b, 
            IEnumerable<T3> c,
            IEnumerable<T4> d
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
            c.Select(x => (object)x),
            d.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1],
                (T3)x[2],
                (T4)x[3]
            );
        });
        private static IEnumerable<(T1, T2, T3, T4, T5)> Permutate<T1, T2, T3, T4, T5>(
            IEnumerable<T1> a, 
            IEnumerable<T2> b, 
            IEnumerable<T3> c,
            IEnumerable<T4> d, 
            IEnumerable<T5> e
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
            c.Select(x => (object)x),
            d.Select(x => (object)x),
            e.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1],
                (T3)x[2],
                (T4)x[3],
                (T5)x[4]
            );
        });
        private static IEnumerable<(T1, T2, T3, T4, T5, T6)> Permutate<T1, T2, T3, T4, T5, T6>(
            IEnumerable<T1> a,
            IEnumerable<T2> b,
            IEnumerable<T3> c,
            IEnumerable<T4> d,
            IEnumerable<T5> e,
            IEnumerable<T6> f
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
            c.Select(x => (object)x),
            d.Select(x => (object)x),
            e.Select(x => (object)x),
            f.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1],
                (T3)x[2],
                (T4)x[3],
                (T5)x[4],
                (T6)x[5]
            );
        });
        private static IEnumerable<(T1, T2, T3, T4, T5, T6, T7)> Permutate<T1, T2, T3, T4, T5, T6, T7>(
            IEnumerable<T1> a, 
            IEnumerable<T2> b, 
            IEnumerable<T3> c,
            IEnumerable<T4> d, 
            IEnumerable<T5> e, 
            IEnumerable<T6> f, 
            IEnumerable<T7> g
        ) 
        =>
        Permutate(new IEnumerable<object>[]
        {
            a.Select(x => (object)x),
            b.Select(x => (object)x),
            c.Select(x => (object)x),
            d.Select(x => (object)x),
            e.Select(x => (object)x),
            f.Select(x => (object)x),
            g.Select(x => (object)x),
        })
        .Select(result => {
            var x = result.ToArray();
            return (
                (T1)x[0],
                (T2)x[1],
                (T3)x[2],
                (T4)x[3],
                (T5)x[4],
                (T6)x[5],
                (T7)x[6]
            );
        });

        #endregion
    }
}
