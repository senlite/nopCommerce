using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

/// <summary>
/// Reads local nopCommerce order/payment/inventory totals directly from the store schema and ERP
/// totals from the configured ERPNext instance. Either side reports Unavailable (never a fabricated
/// match) when it cannot be read, so reconciliation flags a gap rather than a false zero-variance.
/// </summary>
public sealed class NopErpReconciliationDataSource : IErpReconciliationDataSource
{
    // nopCommerce PaymentStatus.Paid.
    private const int PaidPaymentStatusId = 30;

    private readonly INopDataProvider _dataProvider;
    private readonly ErpConnectionOptions _options;
    private readonly HttpClient _httpClient;

    public NopErpReconciliationDataSource(
        INopDataProvider dataProvider,
        ErpConnectionOptions? options = null,
        HttpClient? httpClient = null)
    {
        _dataProvider = dataProvider;
        _options = options ?? ErpConnectionOptions.Current;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<ErpReconciliationTotals> GetLocalTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _dataProvider.QueryAsync<LocalTotalsRow>(
                @"SELECT
    (SELECT COUNT(*) FROM [Order]
        WHERE Deleted = 0 AND PaymentStatusId = @paidStatus
          AND CreatedOnUtc >= @fromUtc AND CreatedOnUtc < @toUtc) AS OrderCount,
    (SELECT COALESCE(SUM(OrderTotal), 0) FROM [Order]
        WHERE Deleted = 0 AND PaymentStatusId = @paidStatus
          AND CreatedOnUtc >= @fromUtc AND CreatedOnUtc < @toUtc) AS PaymentTotal,
    (SELECT COALESCE(SUM(StockQuantity), 0) FROM Product WHERE Deleted = 0) AS InventoryUnits;",
                new DataParameter("paidStatus", PaidPaymentStatusId),
                new DataParameter("fromUtc", fromUtc),
                new DataParameter("toUtc", toUtc));

            var row = rows.First();
            return new ErpReconciliationTotals
            {
                OrderCount = row.OrderCount,
                PaymentTotal = row.PaymentTotal,
                InventoryUnits = row.InventoryUnits,
                IsAvailable = true
            };
        }
        catch
        {
            return ErpReconciliationTotals.Unavailable;
        }
    }

    public async Task<ErpReconciliationTotals> GetErpTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // No ERP configured: report unavailable rather than inventing a matching total.
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return ErpReconciliationTotals.Unavailable;

        try
        {
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/method/check_engine.reconciliation.totals" +
                $"?from={Uri.EscapeDataString(fromUtc.ToString("O"))}&to={Uri.EscapeDataString(toUtc.ToString("O"))}";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            ApplyAuth(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ErpReconciliationTotals.Unavailable;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            return new ErpReconciliationTotals
            {
                OrderCount = ReadInt(root, "orderCount"),
                PaymentTotal = ReadDecimal(root, "paymentTotal"),
                InventoryUnits = ReadInt(root, "inventoryUnits"),
                IsAvailable = true
            };
        }
        catch
        {
            return ErpReconciliationTotals.Unavailable;
        }
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return;

        var token = string.IsNullOrWhiteSpace(_options.ApiSecret)
            ? _options.ApiKey
            : $"{_options.ApiKey}:{_options.ApiSecret}";
        request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
    }

    private static int ReadInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static decimal ReadDecimal(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var parsed) ? parsed : 0m;

    private sealed class LocalTotalsRow
    {
        public int OrderCount { get; set; }
        public decimal PaymentTotal { get; set; }
        public int InventoryUnits { get; set; }
    }
}
