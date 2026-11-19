namespace IspAppApi.Application.Common
{
    public class Result
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }

        public bool IsSuccess => Success;
        public bool IsFailure => !Success;

        protected Result(bool success, string? errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static Result Ok() => new(true, null);

        public static Result Fail(string errorMessage) => new(false, errorMessage);

        public static Result<T> Ok<T>(T data) => Result<T>.Ok(data);

        public static Result<T> Fail<T>(string errorMessage) => Result<T>.Fail(errorMessage);

        public void Deconstruct(out bool success, out string? errorMessage)
        {
            success = Success;
            errorMessage = ErrorMessage;
        }
    }

    public class Result<T> : Result
    {
        public T? Data { get; }
        public T? Value => Data;

        protected Result(bool success, string? errorMessage, T? data)
            : base(success, errorMessage)
        {
            Data = data;
        }

        public static Result<T> Ok(T data) => new(true, null, data);

        public static new Result<T> Fail(string errorMessage) => new(false, errorMessage, default);

        public void Deconstruct(out bool success, out string? errorMessage, out T? data)
        {
            success = Success;
            errorMessage = ErrorMessage;
            data = Data;
        }
    }
}
