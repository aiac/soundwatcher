using System.Diagnostics;

namespace SoundWatcher.Network;

/// <summary>
/// VU meter ballistics - handles peak hold and decay for realistic meter behavior
/// </summary>
public class VuMeterBallistics
{
    private int _currentDisplayLevel;
    private readonly Stopwatch _lastUpdateTime;
    private readonly int _decayTimeMs;
    private readonly object _lock = new();

    public VuMeterBallistics(int decayTimeMs = 150)
    {
        _decayTimeMs = Math.Max(50, Math.Min(500, decayTimeMs)); // Clamp between 50-500ms
        _currentDisplayLevel = 0;
        _lastUpdateTime = Stopwatch.StartNew();
    }

    /// <summary>
    /// Updates the VU meter with a new peak value
    /// Fast attack (instant rise), slow decay (gradual fall)
    /// </summary>
    /// <param name="newPeakLevel">New peak level (0-100)</param>
    /// <returns>Display level after applying ballistics (0-100)</returns>
    public int Update(int newPeakLevel)
    {
        lock (_lock)
        {
            var elapsedMs = _lastUpdateTime.ElapsedMilliseconds;
            _lastUpdateTime.Restart();

            // Fast attack - if new peak is higher, use it immediately
            if (newPeakLevel > _currentDisplayLevel)
            {
                _currentDisplayLevel = newPeakLevel;
            }
            else
            {
                // Slow decay - gradually fall based on elapsed time
                // Decay rate: 100% level decays to 0% in decayTimeMs
                var decayAmount = (int)((100.0 / _decayTimeMs) * elapsedMs);
                _currentDisplayLevel = Math.Max(0, _currentDisplayLevel - decayAmount);

                // If new peak is higher than decayed level, use new peak
                if (newPeakLevel > _currentDisplayLevel)
                {
                    _currentDisplayLevel = newPeakLevel;
                }
            }

            return _currentDisplayLevel;
        }
    }

    /// <summary>
    /// Resets the meter to zero
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _currentDisplayLevel = 0;
            _lastUpdateTime.Restart();
        }
    }
}
