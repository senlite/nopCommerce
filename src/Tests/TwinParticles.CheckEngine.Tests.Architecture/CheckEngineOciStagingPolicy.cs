namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Isolated OCI staging contract for the Check Engine demo store.
/// Must not collide with Senlite (:8000/:8080) or Twin Particles ERP (:8001).
/// </summary>
public static class CheckEngineOciStagingPolicy
{
    public const string PublicHost = "checkengine.senlite.net";
    public const string VmIp = "193.123.90.97";
    public const string RepoRootOnOci = "/opt/checkengine/senlite-commerce";
    public const int LoopbackPort = 8081;
    public const string NetworkName = "checkengine_net";
    public const string WebContainer = "checkengine_web";
    public const string DbContainer = "checkengine_db";
    public const string PostgresImage = "postgres:16";
}
