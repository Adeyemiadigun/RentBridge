using System.Linq.Expressions;

namespace RentBridge.Application.Common.Interfaces;

/// <summary>
/// Generic background-work abstraction. Implementations backed by a real job
/// scheduler (currently Hangfire). Consumers never reference the scheduler;
/// they enqueue typed work and move on.
/// </summary>
public interface IBackgroundJobDispatcher
{
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
    string Enqueue<T>(Expression<Action<T>> methodCall);

    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);
    void RemoveRecurring(string jobId);
}