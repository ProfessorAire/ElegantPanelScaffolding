using System;

namespace EPS.Services
{
    public interface IToastService
    {
        event Action<ToastMessage>? OnToast;
        void Show(string message, ToastLevel level = ToastLevel.Info, int durationSeconds = 4);
        void Success(string message, int durationSeconds = 4);
        void Error(string message, int durationSeconds = 5);
        void Info(string message, int durationSeconds = 4);
    }

    public enum ToastLevel { Info, Success, Warning, Error }

    public record ToastMessage(string Message, ToastLevel Level, int DurationSeconds);

    public class ToastService : IToastService
    {
        public event Action<ToastMessage>? OnToast;

        public void Show(string message, ToastLevel level = ToastLevel.Info, int durationSeconds = 4)
            => OnToast?.Invoke(new ToastMessage(message, level, durationSeconds));

        public void Success(string message, int durationSeconds = 4)
            => Show(message, ToastLevel.Success, durationSeconds);

        public void Error(string message, int durationSeconds = 5)
            => Show(message, ToastLevel.Error, durationSeconds);

        public void Info(string message, int durationSeconds = 4)
            => Show(message, ToastLevel.Info, durationSeconds);
    }
}
