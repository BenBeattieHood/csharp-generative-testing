using PageUp.Functional.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenerativePropertyTesting
{
    public static class ResultExtensions
    {
        public static TOk UnpackOk<TOk, TError>(this Result<TOk, TError> result) =>
            result
            .Match(
                ok: value => value,
                error: value => throw new Exception($"Result is 'Error' ({value}), expecting 'Ok'")
            );

        public static TError UnpackError<TOk, TError>(this Result<TOk, TError> result) =>
            result
            .Match(
                ok: value => throw new Exception($"Result is 'Ok' ({value}), expecting 'Error'"),
                error: value => value
            );
    }
}
