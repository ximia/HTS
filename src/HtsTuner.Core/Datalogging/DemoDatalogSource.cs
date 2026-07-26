using System;
using System.Threading;
using System.Threading.Tasks;

namespace HtsTuner.Core.Datalogging;

/// <summary>
/// Generates plausible datalog frames with no hardware attached, so the UI can
/// be exercised and demoed. Simulates a light rev + part-throttle cruise.
/// </summary>
public sealed class DemoDatalogSource
{
    private readonly Random _rng = new();
    private double _t;

    public event Action<DatalogFrame>? SampleReceived;

    public async Task RunAsync(CancellationToken token)
    {
        var frameNo = 0;
        while (!token.IsCancellationRequested)
        {
            _t += 0.05;
            var throttle = 0.5 + 0.5 * Math.Sin(_t * 0.6);        // 0..1 sweep
            var rpm = 900 + throttle * 6200 + Noise(60);
            var map = 25 + throttle * 70 + Noise(2);              // kPa
            var tps = throttle * 100;
            var afr = 14.7 - throttle * 2.6 + Noise(0.15);        // richer under load
            var ect = 82 + Noise(1.5);
            var iat = 28 + throttle * 6 + Noise(0.8);
            var ign = 12 + throttle * 22 + Noise(0.5);
            var inj = 2.0 + throttle * 9.0 + Noise(0.2);          // ms

            var frame = new DatalogFrame();
            frame[Sensor.Frame] = frameNo++;
            frame[Sensor.Rpm] = Math.Round(rpm);
            frame[Sensor.Map] = Math.Round(map, 1);
            frame[Sensor.Tps] = Math.Round(tps, 1);
            frame[Sensor.Afr] = Math.Round(afr, 2);
            frame[Sensor.Ect] = Math.Round(ect, 1);
            frame[Sensor.Iat] = Math.Round(iat, 1);
            frame[Sensor.IgnFnl] = Math.Round(ign, 1);
            frame[Sensor.InjDur] = Math.Round(inj, 2);
            frame[Sensor.BatV] = Math.Round(13.9 + Noise(0.1), 2);
            frame[Sensor.Vss] = Math.Round(throttle * 120);

            SampleReceived?.Invoke(frame);
            try { await Task.Delay(50, token); } catch (TaskCanceledException) { break; }
        }
    }

    private double Noise(double amp) => (_rng.NextDouble() - 0.5) * 2 * amp;
}
