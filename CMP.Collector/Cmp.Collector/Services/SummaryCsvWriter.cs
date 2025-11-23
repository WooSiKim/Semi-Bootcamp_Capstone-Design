// CMP 런 요약 정보를 스레드 세이프하게 CSV 파일(cmp_runs.csv)에 Append하는 유틸리티.
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Cmp.Collector.Services
{
    /// CSV 누적 저장기 (스레드 세이프)
    ///   - 파일 없으면 헤더 1회 기록
    ///   - 그 이후 Append만 수행
    public sealed class SummaryCsvWriter
    {
        private static readonly object _lock = new object();

        // CSV 파일 경로 (logs/cmp_runs.csv)
        public readonly string CsvPath;

        // CSV 헤더 (스키마)
        private const string Header =
            "id,mean_in,range_in,wiwnu_in,mean_out,range_out,wiwnu_out,start_time,elapsed_sec,status";

        /// rootDir 아래 logs/cmp_runs.csv 로 기록 (rootDir가 logs라면 그대로 사용)
        public SummaryCsvWriter(string rootDir)
        {
            if (string.IsNullOrWhiteSpace(rootDir))
                throw new ArgumentException("rootDir is null or empty", nameof(rootDir));

            Directory.CreateDirectory(rootDir);
            CsvPath = Path.Combine(rootDir, "cmp_runs.csv");

            EnsureHeader();
        }

        /// CSV 한 줄에 대응하는 Row 타입
        public struct Row
        {
            public string Id;
            public double MeanIn, RangeIn, WiwIn;
            public double MeanOut, RangeOut, WiwOut;
            public DateTime StartUtc;
            public double ElapsedSec;
            public string Status;

            public Row(
                string id,
                double meanIn, double rangeIn, double wiwIn,
                double meanOut, double rangeOut, double wiwOut,
                DateTime startUtc, double elapsedSec, string status)
            {
                Id = id ?? "";
                MeanIn = meanIn; RangeIn = rangeIn; WiwIn = wiwIn;
                MeanOut = meanOut; RangeOut = rangeOut; WiwOut = wiwOut;
                StartUtc = startUtc;
                ElapsedSec = elapsedSec;
                Status = string.IsNullOrEmpty(status) ? "ABORT" : status;
            }
        }

        /// CSV에 Row 추가 (헤더 자동 보장)
        public void Append(Row r)
        {
            lock (_lock)
            {
                EnsureHeader();

                var inv = CultureInfo.InvariantCulture;
                // 시간은 로컬표시 YYYY-MM-DD HH:mm:ss
                string line = string.Join(",",
                    Escape(r.Id),
                    r.MeanIn.ToString(inv),
                    r.RangeIn.ToString(inv),
                    r.WiwIn.ToString(inv),
                    r.MeanOut.ToString(inv),
                    r.RangeOut.ToString(inv),
                    r.WiwOut.ToString(inv),
                    r.StartUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    r.ElapsedSec.ToString(inv),
                    Escape(r.Status)
                );

                File.AppendAllText(CsvPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        // ───────── 내부 유틸 ─────────

        private void EnsureHeader()
        {
            // 파일이 없거나 비어 있으면 헤더를 기록
            if (!File.Exists(CsvPath) || new FileInfo(CsvPath).Length == 0)
            {
                File.WriteAllText(CsvPath, Header + Environment.NewLine, Encoding.UTF8);
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // 콤마/인용부호 대비 단순 이스케이프
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
