using App.Metrics;
using App.Metrics.Counter;

public class MetricReporter
{
    private readonly IMetrics _metrics;

    public MetricReporter(IMetrics metrics)
    {
        _metrics = metrics;
    }

    public void RegisterRequest()
    {
        _metrics.Measure.Counter.Increment(MetricsRegistry.RequestCounter);
    }

    public void RegisterTransaction(string type)
    {
        _metrics.Measure.Counter.Increment(
            MetricsRegistry.TransactionCounter,
            new MetricTags("type", type));
    }

    public void RegisterError()
    {
        _metrics.Measure.Counter.Increment(MetricsRegistry.ErrorCounter);
    }
}

public static class MetricsRegistry
{
    public static CounterOptions RequestCounter => new CounterOptions
    {
        Name = "http_requests_total",
        Context = "CapricornApi",
        MeasurementUnit = Unit.Requests
    };

    public static CounterOptions TransactionCounter => new CounterOptions
    {
        Name = "transactions_total",
        Context = "CapricornApi",
        MeasurementUnit = Unit.Transactions
    };

    public static CounterOptions ErrorCounter => new CounterOptions
    {
        Name = "errors_total",
        Context = "CapricornApi",
        MeasurementUnit = Unit.Errors
    };
}