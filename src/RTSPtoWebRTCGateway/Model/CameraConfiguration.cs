namespace RTSPtoWebRTCGateway.Model
{
    public class CameraConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}
