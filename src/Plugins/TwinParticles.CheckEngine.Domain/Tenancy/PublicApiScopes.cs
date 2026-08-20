namespace TwinParticles.CheckEngine.Domain.Tenancy;

public static class PublicApiScopes
{
    public const string VehiclesRead = "vehicles.read";
    public const string VinDecode = "vin.decode";
    public const string OemResolve = "oem.resolve";
    public const string FitmentEvaluate = "fitment.evaluate";
    public const string Search = "search.read";

    public const string WebhooksManage = "webhooks.manage";

    public const string DefaultCsv =
        VehiclesRead + "," + VinDecode + "," + OemResolve + "," + FitmentEvaluate + "," + Search + "," + WebhooksManage;
}
