using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Domain.Common
{
    public class Result
    {
        // Was the operation successful? (true/false)
        public bool IsSuccess { get; }

        // If it failed, what went wrong? (null if it succeeded)
        public string? Error { get; }

        // Private constructor so only the static methods below can create it
        protected Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        // Shortcut to create a successful result (e.g., Result.Ok())
        public static Result Ok() => new(true, null);

        // Shortcut to create a failed result with an error message
        public static Result Fail(string error) => new(false, error);
    }

    public class Result<T> : Result
    {
        // The actual data (e.g., the Email object) if successful
        public T Value { get; }

        // Private constructor for success
        protected Result(T value, bool isSuccess, string? error) : base(isSuccess, error)
        {
            Value = value;
        }

        // Fills in that comment: How to return a success with a value
        public static Result<T> Ok(T value) => new(value, true, null);

        // Fills in that comment: How to return a failure when expecting a type T back
        new public static Result<T> Fail(string error) => new(default!, false, error);
    }
}
