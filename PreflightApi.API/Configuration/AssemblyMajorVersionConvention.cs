using System.Reflection;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace PreflightApi.API.Configuration;

public class AssemblyMajorVersionConvention : IControllerConvention
{
    public bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
    {
        var majorVersion = Assembly.GetEntryAssembly()?.GetName().Version?.Major ?? 1;
        controller.HasApiVersion(new ApiVersion(majorVersion, 0));
        return true;
    }
}
