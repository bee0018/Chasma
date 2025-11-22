using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ChasmaWebApi.Util;

/// <summary>
/// Feature provider used to exclude controllers from the Swagger mapping by the specified type.
/// </summary>
/// <param name="controllerType">The type of the controller to be excluded.</param>
public class ExcludeControllerFeatureProvider(Type controllerType) : IApplicationFeatureProvider<ControllerFeature>
{
    /// <summary>
    /// Finds the specified controller based on its type and removes it from the controller mapping.
    /// </summary>
    /// <param name="parts">The application parts.</param>
    /// <param name="feature">The application controller feature.</param>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        TypeInfo? controller = feature.Controllers.FirstOrDefault(typeInfo => typeInfo.AsType() == controllerType);
        if (controller != null)
        {
            feature.Controllers.Remove(controller);
        }
    }
}