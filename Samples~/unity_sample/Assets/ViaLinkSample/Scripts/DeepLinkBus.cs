using System.Collections.Generic;

namespace ViaLinkSample
{
    /// 딥링크/디퍼드/링크생성/결제 결과를 Bootstrap → Host UI로 전달하는 공유 상태.
    /// (Android의 StateFlow, iOS의 NotificationCenter에 해당)
    public static class DeepLinkBus
    {
        public class Result
        {
            public string Title;
            public string Message;
            public string CopyableText;
        }

        private static readonly Queue<Result> _queue = new Queue<Result>();
        private static readonly object _lock = new object();

        public static void Post(string title, string message, string copyableText = null)
        {
            lock (_lock)
            {
                _queue.Enqueue(new Result
                {
                    Title = title,
                    Message = message,
                    CopyableText = copyableText,
                });
            }
        }

        public static Result TryDequeue()
        {
            lock (_lock)
            {
                return _queue.Count > 0 ? _queue.Dequeue() : null;
            }
        }
    }
}
