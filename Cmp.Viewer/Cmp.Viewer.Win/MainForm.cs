// CMP 텔레메트리 실시간 뷰어의 메인 화면.
//   - Collector의 /live/ws WebSocket에 구독해 TelemetryFrame을 받는다.
//   - Mean / WIWNU / Range / 5존 압력을 실시간 차트로 시각화하고,
//   - 상단 상태 표시 라벨에 현재 웨이퍼 ID와 시작 시간, 초기 메트릭을 보여준다.
using System;
using System.Windows.Forms;
using System.Drawing;
using Cmp.Viewer.Win.Models;
using Cmp.Viewer.Win.Services;
using System.Collections.Concurrent;
using WinFormsTimer = System.Windows.Forms.Timer;
using System.Windows.Forms.DataVisualization.Charting;

namespace Cmp.Viewer.Win
{
    public partial class MainForm : Form
    {
        private const double X_MAX_SEC = 70.0;  // 공정 예상 최대 시간(초)
        private const double MEAN_MIN = 700;    // 두께 평균(Y) 최소
        private const double MEAN_MAX = 800;    // 두께 평균(Y) 최대
        private const double WIW_MIN = 0.0;    // WIWNU 최소
        private const double WIW_MAX = 5.0;    // WIWNU 최대
        private const double RANGE_MIN = 0.0;    // Range 최소
        private const double RANGE_MAX = 50.0;   // Range 최대
        private const double PRESS_MIN = 10.0;    // Z-Press 최소 (kPa 등)
        private const double PRESS_MAX = 40.0;   // Z-Press 최대

        private string? _curWaferId = null;
        private double _lastElapsed = -1;


        private int _rx = 0;
        // ── 연결 URL (필요하면 바꿔)
        private const string WsUrl = "ws://127.0.0.1:8080/live/ws";

        // ── 실시간 큐 & 타이머
        private readonly ConcurrentQueue<TelemetryFrame> _q = new();
        private WsSubscriber? _sub;
        private readonly WinFormsTimer _timer = new() { Interval = 100 };
        public MainForm()
        {
            InitializeComponent();
            InitCharts(); // ← 추가



            // 렌더 타이머
            _timer.Tick += (_, __) => DrainAndRender();
        }

        // 자동 연결
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await ConnectAsync();
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            await DisconnectAsync();
            base.OnFormClosing(e);
        }

        private async System.Threading.Tasks.Task ConnectAsync()
        {
            if (_sub != null) return;
            _sub = new WsSubscriber(WsUrl);
            _sub.OnFrame += f => { _q.Enqueue(f); _rx++; };
            await _sub.StartAsync();
            _timer.Start();
        }

        private async System.Threading.Tasks.Task DisconnectAsync()
        {
            _timer.Stop();
            if (_sub != null) { 
                await _sub.DisposeAsync();
                _sub = null;
                }
        }

        private void DrainAndRender()
        {
            bool updated = false;

            while (_q.TryDequeue(out var f))
            {
                // ── 웨이퍼 변경 감지
                string inId = f.Wafer?.WaferId ?? "";
                bool idChanged = _curWaferId == null || _curWaferId != inId;
                bool timeReset = (_lastElapsed >= 0 && f.ElapsedSec < _lastElapsed);
                bool freshRunStart = (f.ElapsedSec <= 0.25 && _lastElapsed > 1.0); // 0→시작 감지 보정

                if (idChanged || timeReset || freshRunStart)
                {
                    _curWaferId = inId;
                    _lastElapsed = -1;

                    // ① 그래프 전부 클리어
                    foreach (Series s in chartMean.Series) s.Points.Clear();
                    foreach (Series s in chartWiw.Series) s.Points.Clear();
                    foreach (Series s in chartRange.Series) s.Points.Clear();
                    foreach (Series s in chartPress.Series) s.Points.Clear();

                    // ② 축 재적용(안전)
                    ApplyFixedAxes(chartMean, 0, X_MAX_SEC, MEAN_MIN, MEAN_MAX);
                    ApplyFixedAxes(chartWiw, 0, X_MAX_SEC, WIW_MIN, WIW_MAX);
                    ApplyFixedAxes(chartRange, 0, X_MAX_SEC, RANGE_MIN, RANGE_MAX);
                    ApplyFixedAxes(chartPress, 0, X_MAX_SEC, PRESS_MIN, PRESS_MAX);

                    // ③ Status 갱신
                    UpdateStatus(f);
                }

                _lastElapsed = f.ElapsedSec;

                // ── 실시간 포인트 추가 (WIWNU/Range 서로 다른 값 사용 확인)
                AppendPoint(chartMean, "Mean", f.ElapsedSec, f.Metrics.Mean);
                AppendPoint(chartWiw, "WIWNU", f.ElapsedSec, f.Metrics.Wiw);   // ← WIW
                AppendPoint(chartRange, "Range", f.ElapsedSec, f.Metrics.Range); // ← Range

                var ps = f.Metrics.Pressures ?? Array.Empty<double>();
                double P(int i) => (ps.Length > i) ? ps[i] : double.NaN;

                AppendPoint(chartPress, "p1", f.ElapsedSec, P(0));
                AppendPoint(chartPress, "p2", f.ElapsedSec, P(1));
                AppendPoint(chartPress, "p3", f.ElapsedSec, P(2));
                AppendPoint(chartPress, "p4", f.ElapsedSec, P(3));
                AppendPoint(chartPress, "p5", f.ElapsedSec, P(4));

                updated = true;
            }

            if (!updated) return;
            this.Text = $"CMP Viewer — rx:{_rx}";
        }

        // ── 디자이너에 남아있는 이벤트 핸들러 에러 해결용(빈 스텁)
        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e) { }
        private void bottomGrid_Paint(object sender, PaintEventArgs e) { }
        private void MainForm_Load(object sender, EventArgs e) { }
        private void SetupChart(Chart ch, string title, bool showLegend = false)
        {
            ch.Series.Clear(); ch.ChartAreas.Clear(); ch.Titles.Clear();

            var area = new ChartArea("ca");
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(30, 0, 0, 0);
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(30, 0, 0, 0);
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 8f);
            area.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 8f);
            ch.ChartAreas.Add(area);

            ch.AntiAliasing = AntiAliasingStyles.None;
            ch.TextAntiAliasingQuality = TextAntiAliasingQuality.Normal;

            ch.Titles.Add(title);
            ch.Titles[0].Font = new System.Drawing.Font("Segoe UI Semibold", 9f);
            if (!showLegend) ch.Legends.Clear();
        }

        private Series AddLine(Chart ch, string name, System.Drawing.Color color)
        {
            var s = new Series(name)
            {
                ChartType = SeriesChartType.FastLine,
                ChartArea = "ca",
                XValueType = ChartValueType.Double,
                IsXValueIndexed = false,
                Color = color,
                BorderWidth = 2
            };
            ch.Series.Add(s);
            return s;
        }

        private void InitCharts()
        {
            // Mean, WIW, Range — 기존 유지
            SetupChart(chartMean, "Mean");
            SetupChart(chartWiw, "WIWNU");
            SetupChart(chartRange, "Range");

            // ✅ 압력 그래프는 Legend 표시 활성화
            SetupChart(chartPress, "Zonal Pressure (p1~p5)", showLegend: true);

            // ───── 색상 팔레트 개선 ─────
            AddLine(chartMean, "Mean", System.Drawing.Color.CornflowerBlue);
            AddLine(chartWiw, "WIWNU", System.Drawing.Color.MediumVioletRed);
            AddLine(chartRange, "Range", System.Drawing.Color.OrangeRed);

            // ✅ 존별 색상 재지정 (더 명확하게 구분)
            AddLine(chartPress, "p1", System.Drawing.Color.Red);      // Zone1
            AddLine(chartPress, "p2", System.Drawing.Color.Orange);   // Zone2
            AddLine(chartPress, "p3", System.Drawing.Color.Yellow);   // Zone3
            AddLine(chartPress, "p4", System.Drawing.Color.Green);    // Zone4
            AddLine(chartPress, "p5", System.Drawing.Color.Blue);     // Zone5

            // ✅ 압력 그래프 범례 스타일 (한 줄 + 폭 확장 + 수동 배치)
            var lg = chartPress.Legends[0];
            lg.IsDockedInsideChartArea = false;
            lg.LegendStyle = LegendStyle.Row;
            lg.TableStyle = LegendTableStyle.Wide;
            lg.Font = new System.Drawing.Font("Segoe UI", 8f);
            lg.Alignment = StringAlignment.Center;
            lg.MaximumAutoSize = 100;
            lg.Position.Auto = false;
            lg.Position = new ElementPosition(0, 92, 100, 8); // 범례를 차트 하단에 고정 (겹치지 않음)

            // ✅ 그래프 자체 높이는 그대로 두고, 아래쪽에 범례 공간만 확보
            var area = chartPress.ChartAreas["ca"];
            area.Position.Auto = false;
            area.Position.X = 8f;      // 왼쪽 여백
            area.Position.Y = 5f;      // 위쪽 그대로
            area.Position.Width = 88f;
            area.Position.Height = 86f; // 아래쪽 여백 확보 (그래프는 그대로)

            // ✅ 축/눈금 스타일 약간 조정 (범례와 시각적으로 분리)
            area.AxisX.IsLabelAutoFit = false;
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 8f);
            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.Size = 1.5f;

            // ✅ 나머지 그래프 축 설정 유지
            ApplyFixedAxes(chartMean, 0, X_MAX_SEC, MEAN_MIN, MEAN_MAX);
            ApplyFixedAxes(chartWiw, 0, X_MAX_SEC, WIW_MIN, WIW_MAX);
            ApplyFixedAxes(chartRange, 0, X_MAX_SEC, RANGE_MIN, RANGE_MAX);
            ApplyFixedAxes(chartPress, 0, X_MAX_SEC, PRESS_MIN, PRESS_MAX);
        }
        private void AppendPoint(Chart ch, string seriesName, double tSec, double y)
        {
            ch.Series[seriesName].Points.AddXY(tSec, y);
            ch.Invalidate(); // 빠른 리페인트
        }

        private static void ApplyFixedAxes(Chart ch, double xmin, double xmax, double ymin, double ymax)
        {
            var area = ch.ChartAreas["ca"];
            area.AxisX.Minimum = xmin;
            area.AxisX.Maximum = xmax;
            area.AxisY.Minimum = ymin;
            area.AxisY.Maximum = ymax;

            area.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            area.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
        }

        private void UpdateStatus(TelemetryFrame f)
        {
            // Wafer ID
            lblWaferId.Text = string.IsNullOrWhiteSpace(f.Wafer?.WaferId) ? "-" : f.Wafer.WaferId;

            // Start Time (Ts가 DateTime 가정)
            var ts = f.Ts;
            if (ts.Kind == DateTimeKind.Unspecified)
                ts = DateTime.SpecifyKind(ts, DateTimeKind.Local);
            lblStartTime.Text = ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

            // 초기 메트릭
            lblInitMean.Text = f.Metrics.Mean.ToString("0.###");
            lblInitRange.Text = f.Metrics.Range.ToString("0.###");
            lblInitWiw.Text = f.Metrics.Wiw.ToString("0.###");
        }



    }
}
