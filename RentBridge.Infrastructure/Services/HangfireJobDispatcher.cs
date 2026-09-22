using Hangfire;
using System.Linq.Expressions;
using RentBridge.Application.Common.Interfaces;

namespace RentBridge.Infrastructure.Services;

/// <summary>
/// Hangfire-backed implementation of <see cref="IBackgroundJobDispatcher"/>.
/// Storage/server setup lives in the API host; this is purely the enqueue surface.
/// </summary>
public class HangfireJobDispatcher(
    IBackgroundJobClient backgroundJobs,
    IRecurringJobManager recurringJobs) : IBackgroundJobDispatcher
{
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        => backgroundJobs.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall)
        => backgroundJobs.Enqueue(methodCall);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        => backgroundJobs.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
        => backgroundJobs.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
        => recurringJobs.AddOrUpdate<T>(jobId, methodCall, cronExpression, new RecurringJobOptions());

    public void RemoveRecurring(string jobId)
        => recurringJobs.RemoveIfExists(jobId);
}