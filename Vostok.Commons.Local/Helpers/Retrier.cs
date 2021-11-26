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
                    onRetryAttempt?.Invoke();
                    if (i == maxTriesCount)
                        throw new Exception(failMessage, e);
                    continue;
                }

                break;
            }
        }
    }
}