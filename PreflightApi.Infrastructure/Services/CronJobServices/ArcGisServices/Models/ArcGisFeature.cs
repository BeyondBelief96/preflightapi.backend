namespace PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices.Models
{
    public class ArcGisFeature<T>
    {
        public T Attributes { get; set; } = default!;
        public ArcGisGeometry? Geometry { get; set; }
    }
}
