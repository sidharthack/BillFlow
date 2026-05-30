using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace BillFlow.Gateway.Transforms;

/// <summary>
/// Programmatic YARP transforms applied to every proxied request/response.
/// Registered via AddTransformFactory() in Program.cs.
/// </summary>
public class GatewayTransformProvider : ITransformFactory
{
    private const string GatewayVersion = "1.0.0";

    // 1. Fix: Implement the correct Validate method for appsettings.json validation
    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        // We aren't defining custom JSON transform keywords, so just return true
        return true;
    }

    // 2. Fix: Implement the correct Build method signature 
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        // Return false because we aren't handling custom JSON keywords here
        return false;
    }

    // 3. This runs on EVERY route automatically, keeping your global headers intact
    public void Apply(TransformBuilderContext context)
    {
        // Add X-Gateway-Version to every forwarded request
        context.AddRequestHeader(
            "X-Gateway-Version",
            GatewayVersion,
            append: false);

        // Add X-Forwarded-Host so services know the original host
        context.AddOriginalHost(useOriginal: true);

        // Remove internal headers the client should never send
        context.AddRequestHeaderRemove("X-Internal-Service");
        context.AddRequestHeaderRemove("X-Service-Name");
    }
}