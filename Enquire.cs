using PageUp.Functional.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using Void = PageUp.Functional.Core.Void;

namespace GenerativePropertyTesting
{
    public static class Enquire
    {
        private static (Func<TOk, Result<TOk, Exception>> Ok, Func<Exception, Result<TOk, Exception>> Error) GetValueOrExceptionCtors<TOk>() => Result<TOk, Exception>.GetBaseConstructors();
        
        public static Result<TResult, Exception> AsCaught<TResult>(Func<TResult> f)
        {
            var Result = GetValueOrExceptionCtors<TResult>();
            try 
            {
                return Result.Ok(f());
            }
            catch (Exception ex)
            {
                return Result.Error(ex);
            }
        }

        


        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test,
            Action<TResult> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: test.Compile()(data),
            assert: assert
        );

        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            string title,
            TResult result,
            Action<TData, TResult> assert
            ) 
        =>
        Test(
            data: data,
            title: title,
            result: result,
            assert: x => assert(data, x)
        );

        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test,
            Action<TData, TResult> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: test.Compile()(data),
            assert: result => assert(data, result)
        );
        
        public static Func<TData, TestResult<TData, TResult, Exception>> Test<TData, TResult>(
            Expression<Func<TData, TResult>> test,
            Action<TData, TResult> assert
            ) 
        => 
        (TData data) =>
        Test(
            data: data,
            title: test.ToString(),
            result: test.Compile()(data),
            assert: result => assert(data, result)
        );
        
        // asd
        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            string title,
            TResult result
            ) 
        => 
        Test(
            data: data,
            title: title, 
            result: result,
            assert: x => { }
        );
        
        public static TestResult<TData, Void, Exception> Test<TData>(
            TData data,
            Expression<Action<TData>> test
            )
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: Prelude.ToVoid(test.Compile())(data)
        );
        
        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test
            ) 
        => 
        Test(
            data: data,
            title: test.ToString(),
            result: test.Compile()(data)
        );
        
        public static Func<TData, TestResult<TData, TResult, Exception>> Test<TData, TResult>(
            Expression<Func<TData, TResult>> test
            ) 
        => 
        (TData data)
        =>
        Test(
            data: data,
            test: test
        );
        
        //private static TestResult<TData, Result<TResult, TError>, Exception> Testc<TData, TResult, TError>(
        //    TData data,
        //    Result<TResult, TError> result
        //    ) 
        //=> 
        //Testc(
        //    data: data,
        //    result: result,
        //    assert: x => { x.UnpackOk(); }
        //);

        public static TestResult<TData, TResult, Exception> Test<TData, TResult>(
            TData data,
            string title,
            TResult result,
            Action<TResult> assert
            ) 
        {
            var TestResultFactory = TestResult<TData, TResult, Exception>.GetBaseConstructors();

            return
                AsCaught(Prelude.ToVoid(() => assert(result)))
                .Match(
                    ok: _ => TestResultFactory.Success(title, data, result),
                    error: ex => TestResultFactory.Failure(title, data, ex)
                );
        }


        
        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test,
            Action<Result<TResult, Exception>> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()(data)),
            assert: assert
        );

        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TResult>> test,
            Action<TData, Result<TResult, Exception>> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()()),
            assert: x => assert(data, x)
        );

        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test,
            Action<TData, Result<TResult, Exception>> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()(data)),
            assert: result => assert(data, result)
        );
        
        public static Func<TData, TestResult<TData, Result<TResult, Exception>, Exception>> TestC<TData, TResult>(
            Expression<Func<TData, TResult>> test,
            Action<TData, Result<TResult, Exception>> assert
            ) 
        => 
        (TData data) =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()(data)),
            assert: result => assert(data, result)
        );
        
        //asd
        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TResult>> test
            ) 
        => 
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()()),
            assert: x => { }
        );
        
        public static TestResult<TData, Result<Void, Exception>, Exception> TestC<TData>(
            TData data,
            Expression<Action<TData>> test
            )
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => Prelude.ToVoid(test.Compile())(data))
        );
        
        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TData, TResult>> test
            ) 
        => 
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()(data))
        );
        
        public static Func<TData, TestResult<TData, Result<TResult, Exception>, Exception>> TestC<TData, TResult>(
            Expression<Func<TData, TResult>> test
            ) 
        => 
        (TData data)
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()(data))
        );
        
        //private static TestResult<TData, Result<TResult, TError>, Exception> Testc<TData, TResult, TError>(
        //    TData data,
        //    Result<TResult, TError> result
        //    ) 
        //=> 
        //Testc(
        //    data: data,
        //    result: result,
        //    assert: x => { x.UnpackOk(); }
        //);

        public static TestResult<TData, Result<TResult, Exception>, Exception> TestC<TData, TResult>(
            TData data,
            Expression<Func<TResult>> test,
            Action<Result<TResult, Exception>> assert
            ) 
        =>
        Test(
            data: data,
            title: test.ToString(),
            result: AsCaught(() => test.Compile()())
        );
    }
}
