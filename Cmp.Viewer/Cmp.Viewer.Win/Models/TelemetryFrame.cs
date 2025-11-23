namespace Cmp.Viewer.Win.Models
{
    public sealed class TelemetryFrame
    {
        public string Schema { get; set; } = "cmp.telemetry.v1";
        public string DeviceId { get; set; } = "";
        public string RunId { get; set; } = "";
        public long Seq { get; set; }
        public System.DateTime Ts { get; set; }
        public double ElapsedSec { get; set; }   // 시간축(초)
        public string Phase { get; set; } = "";

        public MetricsBlock Metrics { get; set; } = new();
        public WaferBlock Wafer { get; set; } = new();

        public sealed class MetricsBlock
        {
            public double Mean { get; set; }
            public double Range { get; set; }
            public double Wiw { get; set; }
            public double[] Pressures { get; set; } = System.Array.Empty<double>(); // p1..p5
        }

        public sealed class WaferBlock
        {
            public string LotId { get; set; } = "";
            public string WaferId { get; set; } = "";
        }
    }
}
