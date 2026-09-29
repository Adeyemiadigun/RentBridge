namespace RentBridge.Application.Common;

/// <summary>
/// One server-side GROUP BY bucket: the key and its row count.
/// Buckets with zero rows are absent — read them with GetValueOrDefault.
/// </summary>
public sealed record GroupCount<TKey>(TKey Key, int Count);
