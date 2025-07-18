using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NearbyCS_API.Uiltities
{
    public class AddShowDebugHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "showdebug",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema { Type = "boolean" },
                Description = "Optional debug flag. Set to true to view debug telemetry"
            });
        }
    }
}
