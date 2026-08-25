using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using System.Runtime.ExceptionServices;

namespace DHBIMWATER.Revit.Commands;

/// <summary>모델리스 UI 스레드의 Revit API 읽기를 ExternalEvent로 동기 마샬링한다.</summary>
internal sealed class RevitDispatcher : IRevitDispatcher, IDisposable
{
    private readonly object _runSync = new();
    private readonly RevitDispatcherHandler _handler = new();
    private readonly ExternalEvent _externalEvent;
    private readonly int _revitThreadId;
    private bool _disposed;

    public RevitDispatcher()
    {
        _revitThreadId = Environment.CurrentManagedThreadId;
        _externalEvent = ExternalEvent.Create(_handler);
    }

    public void Run(Action action) => Run<object?>(() =>
    {
        action();
        return null;
    });

    public T Run<T>(Func<T> func)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Environment.CurrentManagedThreadId == _revitThreadId) return func();

        lock (_runSync)
        {
            using var request = new RevitDispatchRequest<T>(func);
            _handler.SetPending(request.Execute);
            var status = _externalEvent.Raise();
            if (status is ExternalEventRequest.Denied or ExternalEventRequest.TimedOut)
            {
                _handler.ClearPending(request.Execute);
                throw new InvalidOperationException($"Revit 요청을 등록하지 못했습니다: {status}");
            }

            request.Wait();
            return request.GetResult();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _externalEvent.Dispose();
    }

    private sealed class RevitDispatcherHandler : IExternalEventHandler
    {
        private Action? _pending;

        public void SetPending(Action action)
        {
            if (Interlocked.CompareExchange(ref _pending, action, null) is not null)
                throw new InvalidOperationException("처리되지 않은 Revit 요청이 남아 있습니다.");
        }

        public void ClearPending(Action action) => Interlocked.CompareExchange(ref _pending, null, action);

        public void Execute(UIApplication app) => Interlocked.Exchange(ref _pending, null)?.Invoke();
        public string GetName() => "DHBIMWATER Revit Dispatcher";
    }

    private sealed class RevitDispatchRequest<T> : IDisposable
    {
        private readonly Func<T> _func;
        private readonly ManualResetEventSlim _completed = new(false);
        private T? _result;
        private ExceptionDispatchInfo? _error;

        public RevitDispatchRequest(Func<T> func) => _func = func;

        public void Execute()
        {
            try { _result = _func(); }
            catch (Exception ex) { _error = ExceptionDispatchInfo.Capture(ex); }
            finally { _completed.Set(); }
        }

        public void Wait() => _completed.Wait();

        public T GetResult()
        {
            _error?.Throw();
            return _result!;
        }

        public void Dispose() => _completed.Dispose();
    }
}
