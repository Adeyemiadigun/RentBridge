using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Domain.Common
{
    /// <summary>
    /// How a failure should be reported over HTTP. Lets a handler say "you may
    /// not do this" without the controller having to pattern-match on the
    /// message, so the published API contract and the response actually agree.
    /// </summary>
    public enum ResultErrorKind
    {
        /// <summary>Business-rule violation or bad input. Maps to 400.</summary>
        Invalid = 0,

        /// <summary>Authenticated, but not permitted to do this. Maps to 403.</summary>
        Forbidden,

        /// <summary>The addressed resource does not exist. Maps to 404.</summary>
        NotFound,

        /// <summary>The resource is not in a state that allows this. Maps to 409.</summary>
        Conflict,
    }

    public class Result
    {
        // Was the operation successful? (true/false)
        public bool IsSuccess { get; }

        // If it failed, what went wrong? (null if it succeeded)
        public string? Error { get; }

        // How the failure should be classified. Always Invalid on success.
        public ResultErrorKind ErrorKind { get; }

        // Private constructor so only the static methods below can create it
        protected Result(bool isSuccess, string? error, ResultErrorKind errorKind)
        {
            IsSuccess = isSuccess;
            Error = error;
            ErrorKind = errorKind;
        }

        // Shortcut to create a successful result (e.g., Result.Ok())
        public static Result Ok() => new(true, null, ResultErrorKind.Invalid);

        // Shortcut to create a failed result with an error message
        public static Result Fail(string error) => new(false, error, ResultErrorKind.Invalid);

        // The caller is known, but not allowed to perform this action.
        public static Result Forbid(string error) => new(false, error, ResultErrorKind.Forbidden);

        public static Result NotFound(string error) => new(false, error, ResultErrorKind.NotFound);

        public static Result Conflict(string error) => new(false, error, ResultErrorKind.Conflict);
    }

    public class Result<T> : Result
    {
        // The actual data (e.g., the Email object) if successful
        public T Value { get; }

        // Private constructor for success
        protected Result(T value, bool isSuccess, string? error, ResultErrorKind errorKind)
            : base(isSuccess, error, errorKind)
        {
            Value = value;
        }

        // Fills in that comment: How to return a success with a value
        public static Result<T> Ok(T value) => new(value, true, null, ResultErrorKind.Invalid);

        // Fills in that comment: How to return a failure when expecting a type T back
        new public static Result<T> Fail(string error) => new(default!, false, error, ResultErrorKind.Invalid);

        new public static Result<T> Forbid(string error) => new(default!, false, error, ResultErrorKind.Forbidden);

        new public static Result<T> NotFound(string error) => new(default!, false, error, ResultErrorKind.NotFound);

        new public static Result<T> Conflict(string error) => new(default!, false, error, ResultErrorKind.Conflict);
    }
}
