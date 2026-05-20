using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace BillFlow.Gateway.Transforms;

/// <summary>
/// Programmatic YARP transforms applied to every proxied request/response.
/// Registered via AddTransforms() in Program.cs.
/// </summary>
public class GatewayTransformProvider : ITransformProvider
{
    private const string GatewayVersion = "1.0.0";

    public void ValidateRoute(TransformRouteValidationContext context) { }
    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        // Add X-Gateway-Version to every forwarded request
        // Downstream services can log this for debugging
        context.AddRequestHeader(
            "X-Gateway-Version",
            GatewayVersion,
            append: false);

        // Add X-Forwarded-Host so services know the original host
        context.AddOriginalHost(useOriginal:true);

        // Remove internal headers the client should never send
        // (prevents header injection attacks)
        context.AddRequestHeaderRemove("X-Internal-Service");
        context.AddRequestHeaderRemove("X-Service-Name");
    }
}