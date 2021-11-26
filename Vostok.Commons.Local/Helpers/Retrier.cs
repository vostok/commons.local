using System;

namespace Vostok.Commons.Local.Helpers
{
    internal static class Retrier
    {
        public static void RetryOnException(Action action, int maxTriesCount, string failMessage, Action onRetryAttempt = null)
        {
            for (var i = 1; ; i++)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    if (i == maxTriesCount)
                        throw new Exception(failMessage, e);
                    onRetryAttempt?.Invoke();
                    continue;
                }

                break;
            }
        }
    }
}