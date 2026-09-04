namespace Divar_UWP.Infrastructure
{
    public sealed class ServiceResult<T>
    {
        private ServiceResult(bool isSuccess, T value, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool IsSuccess { get; private set; }

        public T Value { get; private set; }

        public string ErrorMessage { get; private set; }

        public static ServiceResult<T> Success(T value)
        {
            return new ServiceResult<T>(true, value, string.Empty);
        }

        public static ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T>(false, default(T), errorMessage);
        }
    }
}
