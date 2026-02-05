namespace PreflightApi.Domain.ValueObjects.GAirmets
{
    public class GAirmetArea
    {
        public int NumPoints { get; set; }
        public List<GAirmetPoint> Points { get; set; } = new();
    }
}
