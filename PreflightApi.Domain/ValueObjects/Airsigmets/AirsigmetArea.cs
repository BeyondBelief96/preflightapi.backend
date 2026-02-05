namespace PreflightApi.Domain.ValueObjects.Airsigmets
{
    public class AirsigmetArea
    {
        public int NumPoints { get; set; }
        public List<AirsigmetPoint> Points { get; set; } = new();
    }
}
