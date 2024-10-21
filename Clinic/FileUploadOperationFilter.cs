using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
namespace Clinic
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the action has a parameter of type IFormFile or IEnumerable<IFormFile>
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IEnumerable<IFormFile>));

            if (fileParameters.Any())
            {
                // Specify that the request body is of type multipart/form-data
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParameters.ToDictionary(
                                    param => param.Name,
                                    param => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                )
                            }
                        }
                    }
                };
            }
        }
    }
}
