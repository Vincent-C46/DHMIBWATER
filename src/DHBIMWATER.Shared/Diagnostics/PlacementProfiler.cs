using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace DHBIMWATER.Shared.Diagnostics;

/// <summary>
/// 관로 배치 파이프라인의 단계별 소요시간을 누적해 로그 파일로 남기는 임시 계측기.
/// (2026-08-24 장거리 배치 성능 병목 비중 확정용. 병목 확정 후 제거 검토 — TODO)
///
/// 세션이 열려 있을 때만 동작한다. 열지 않으면 Step/Count는 아무 것도 하지 않으므로
/// 단위 테스트나 다른 경로에는 영향이 없다.
/// </summary>
public static class PlacementProfiler
{
    private static readonly object Lock = new();
    private static Session? _current;

    /// <summary>지금 계측 중인지 여부. 로그 문자열을 미리 만들지 않으려는 호출부용.</summary>
    public static bool IsActive => Volatile.Read(ref _current) is not null;

    /// <summary>계측 세션을 연다. 이미 열려 있으면 기존 세션을 버리고 새로 연다.</summary>
    public static void Begin(string title)
    {
        lock (Lock) _current = new Session(title);
    }

    /// <summary>세션을 닫고 보고서를 파일에 기록한 뒤, 같은 내용을 문자열로 돌려준다. 세션이 없으면 null.</summary>
    public static string? End()
    {
        Session? session;
        lock (Lock)
        {
            session = _current;
            _current = null;
        }
        if (session is null) return null;

        var report = session.BuildReport();
        TryWrite(report);
        return report;
    }

    /// <summary>단계 소요시간을 누적한다. 같은 이름으로 여러 번 호출하면 합산·호출횟수가 쌓인다.</summary>
    public static Scope Step(string name) => new(name, Volatile.Read(ref _current));

    /// <summary>
    /// 인스턴스 순번을 구간(기본 100개)으로 묶어 누적한다.
    /// 개당 비용이 일정한지(패밀리가 무거움) 뒤로 갈수록 커지는지(문서 전체 재생성) 구분하기 위한 것.
    /// </summary>
    public static Scope StepBucketed(string name, int index, int bucketSize = 100)
    {
        var session = Volatile.Read(ref _current);
        if (session is null) return default;
        var start = index / bucketSize * bucketSize;
        return new Scope($"{name} [{start:0000}~]", session);
    }

    /// <summary>수량(인스턴스 개수 등)을 누적한다.</summary>
    public static void Count(string name, long value)
    {
        var session = Volatile.Read(ref _current);
        if (session is null) return;
        lock (Lock) session.AddCount(name, value);
    }

    /// <summary>보고서를 남길 로그 파일 경로.</summary>
    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DHBIMWATER", "Logs", "placement-profile.log");

    private static void TryWrite(string report)
    {
        try
        {
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, report + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // 계측 실패가 배치 작업을 막으면 안 된다.
        }
    }

    /// <summary>using 블록 하나가 단계 하나. 루프 안에서 쓰이므로 힙 할당이 없는 struct로 둔다.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly Session? _session;
        private readonly long _startTicks;

        internal Scope(string name, Session? session)
        {
            _session = session;
            _name = session is null ? null : name;
            _startTicks = session is null ? 0 : Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_session is null || _name is null) return;
            var elapsedTicks = Stopwatch.GetTimestamp() - _startTicks;
            lock (Lock) _session.AddStep(_name, elapsedTicks);
        }
    }

    internal sealed class Session
    {
        private readonly string _title;
        private readonly DateTime _startedAt = DateTime.Now;
        private readonly Stopwatch _wall = Stopwatch.StartNew();
        private readonly Dictionary<string, (long Ticks, long Calls)> _steps = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);

        internal Session(string title) => _title = title;

        internal void AddStep(string name, long ticks)
        {
            _steps.TryGetValue(name, out var current);
            _steps[name] = (current.Ticks + ticks, current.Calls + 1);
        }

        internal void AddCount(string name, long value)
        {
            _counts.TryGetValue(name, out var current);
            _counts[name] = current + value;
        }

        internal string BuildReport()
        {
            _wall.Stop();
            var msPerTick = 1000d / Stopwatch.Frequency;
            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            sb.AppendLine("==================================================");
            sb.AppendLine($"[{_startedAt:yyyy-MM-dd HH:mm:ss}] 관로 배치 계측 — {_title}");
            sb.AppendLine($"전체 소요: {_wall.Elapsed.TotalSeconds.ToString("0.000", culture)}s");

            if (_counts.Count > 0)
            {
                sb.AppendLine("-- 수량 --");
                foreach (var pair in _counts.OrderBy(x => x.Key, StringComparer.Ordinal))
                    sb.AppendLine($"  {pair.Key,-32} {pair.Value,10}");
            }

            if (_steps.Count == 0) return sb.ToString();

            var totalMs = _wall.Elapsed.TotalMilliseconds;
            sb.AppendLine("-- 단계별 (이름순) --");
            sb.AppendLine($"  {"단계",-32} {"합계(ms)",12} {"호출",8} {"평균(ms)",12} {"비중",7}");
            foreach (var pair in _steps.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var ms = pair.Value.Ticks * msPerTick;
                var avg = pair.Value.Calls == 0 ? 0d : ms / pair.Value.Calls;
                var share = totalMs <= 0 ? 0d : ms / totalMs * 100d;
                sb.AppendLine($"  {pair.Key,-32} {ms.ToString("0.0", culture),12} {pair.Value.Calls,8} {avg.ToString("0.000", culture),12} {share.ToString("0.0", culture) + "%",7}");
            }
            return sb.ToString();
        }
    }
}
