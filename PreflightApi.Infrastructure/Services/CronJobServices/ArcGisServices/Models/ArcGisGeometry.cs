namespace PreflightApi.Infrastructure.Services.CronJobServices.ArcGisServices.Models
{
    public class ArcGisGeometry
    {
        public List<double[]>[] Rings { get; set; } = Array.Empty<List<double[]>>();
    }
}
