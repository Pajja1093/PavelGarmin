using System.Text.Json.Serialization;

namespace MyGarmin.Components.Models
{
    public class ActivityDTO
    {
        private double? _distanceKm;
        private double? _speedKmh;

        public long ActivityId { get; set; }
        public string? ActivityName { get; set; }
        public string EventType { get; set; } = "";
        public string? StartTimeLocal { get; set; }
        public double? Distance
        {
            get => _distanceKm;
            set => _distanceKm = value / 1000.0;
        }
        public double? AverageSpeed
        {
            get => _speedKmh;
            set => _speedKmh = value * 3.6;
        }

        private TimeSpan? _duration;

        public double? DurationSeconds
        {
            get => _duration?.TotalSeconds;
            set => _duration = value is null ? null : TimeSpan.FromSeconds(value.Value);
        }

        public TimeSpan? Duration => _duration;

        public double? AverageHR { get; set; }
        public double? Calories { get; set; }
        public bool IsPR { get; set; }
        public bool IsFavorite { get; set; }
    }
}
