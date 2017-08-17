using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swagger.Filters
{
    /// <summary>
    ///     Marks parameters annotated with <see cref="RequiredAttribute" /> as being required in the generated Swagger
    ///     document.
    /// </summary>
    public class RequiredAttributeOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null) return;

            IEnumerable<ControllerParameterDescriptor> actualParameters =
                context.ApiDescription.ActionDescriptor.Parameters.Cast<ControllerParameterDescriptor>();

            // The actual parameter information is transformed into a dictionary
            // with the key being the name of the parameter
            // and the value being whether the parameter is required.
            Dictionary<string, bool> parameterRequired = actualParameters
                .GroupBy(ProperName, HasAttribute<RequiredAttribute>)
                .ToDictionary(_ => _.Key, _ => _.First());

            foreach (IParameter p in operation.Parameters) p.Required = p.Required || parameterRequired[p.Name];
        }

        private static bool HasAttribute<T>(ControllerParameterDescriptor self)
        {
            return self.ParameterInfo.CustomAttributes.Any(_ => _.AttributeType == typeof(T));
        }

        /// <summary>
        ///     Returns the proper name for the parameter. This is either the name of the variable,
        ///     or in the case of using any attributes in the From* family,
        ///     the name is the value of the Name parameter (if set).
        /// </summary>
        /// <param name="self">
        ///     The parameter description.
        /// </param>
        /// <returns>The proper name for the parameter.</returns>
        private static string ProperName(ControllerParameterDescriptor self)
        {
            foreach (var attribute in self.ParameterInfo.CustomAttributes)
            {
                var attributeType = attribute.AttributeType;
                if (attributeType == typeof(FromQueryAttribute)
                    || attributeType == typeof(FromFormAttribute)
                    || attributeType == typeof(FromRouteAttribute)
                    || attributeType == typeof(FromHeaderAttribute))
                {
                    if (attribute.NamedArguments.Count == 0) return self.Name;

                    return (string)attribute.NamedArguments[0].TypedValue.Value;
                }
            }

            return self.Name;
        }
    }
}