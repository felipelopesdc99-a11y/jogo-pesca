using System;
using System.Globalization;

namespace FishingIdle.Texts
{
    /// <summary>
    /// Brazilian formatting for everything a person reads: decimal comma, dot as thousands
    /// separator, DD/MM/AAAA dates and 24h times.
    /// </summary>
    /// <remarks>
    /// Implemented by hand on top of the invariant culture instead of relying on the "pt-BR"
    /// CultureInfo, because culture data is not guaranteed to exist in every Unity player build.
    /// </remarks>
    public static class Format
    {
        /// <summary>1234567 → "1.234.567".</summary>
        public static string Number(long value)
        {
            return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
        }

        /// <summary>
        /// Big amounts in short form (A-124): below a million the full number ("845.320"); then
        /// "12,4 mi", "3,2 bi", "1,5 tri" (one decimal, dropped when it is ",0").
        /// </summary>
        public static string Short(long value)
        {
            var abs = Math.Abs(value);
            if (abs < 1_000_000L)
            {
                return Number(value);
            }

            double scaled;
            string unit;
            if (abs >= 1_000_000_000_000L) { scaled = value / 1e12; unit = " tri"; }
            else if (abs >= 1_000_000_000L) { scaled = value / 1e9; unit = " bi"; }
            else { scaled = value / 1e6; unit = " mi"; }

            // Truncated, never rounded up: "999,9 mi" must not read as "1.000 mi".
            var truncated = Math.Truncate(scaled * 10.0) / 10.0;
            var text = Decimal(truncated, 1);
            if (text.EndsWith(",0", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 2);
            }

            return text + unit;
        }

        /// <summary>35.25 with 1 decimal → "35,3"; 1234.5 → "1.234,5".</summary>
        public static string Decimal(double value, int decimals)
        {
            var pattern = decimals <= 0 ? "#,0" : "#,0." + new string('0', decimals);
            var invariant = value.ToString(pattern, CultureInfo.InvariantCulture);
            // Swap separators through a placeholder so neither replacement clobbers the other.
            return invariant.Replace(",", "\u0001").Replace(".", ",").Replace("\u0001", ".");
        }

        /// <summary>0.103 → "10,3%".</summary>
        public static string Percent(double ratio, int decimals)
        {
            return Decimal(ratio * 100.0, decimals) + "%";
        }

        /// <summary>Centimetres, with one decimal: "35,2 cm".</summary>
        public static string SizeCm(double centimetres)
        {
            return Decimal(centimetres, 1) + " cm";
        }

        /// <summary>"28/09/2026".</summary>
        public static string Date(DateTime local)
        {
            return local.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>"28/09/2026 21:55".</summary>
        public static string DateTime(DateTime local)
        {
            return local.ToString("dd'/'MM'/'yyyy HH':'mm", CultureInfo.InvariantCulture);
        }

        /// <summary>"21:55" (24 h).</summary>
        public static string Time(DateTime local)
        {
            return local.ToString("HH':'mm", CultureInfo.InvariantCulture);
        }

        /// <summary>Unix milliseconds (UTC) rendered in the machine's local time zone.</summary>
        public static string DateTimeFromUnixMs(long unixMs)
        {
            return DateTime(DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime);
        }

        /// <summary>A countdown: "0:07", "12:30", "1:02:03".</summary>
        public static string Countdown(double seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var total = (long)Math.Ceiling(seconds);
            var hours = total / 3600;
            var minutes = (total % 3600) / 60;
            var secs = total % 60;
            return hours > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", hours, minutes, secs)
                : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", minutes, secs);
        }

        /// <summary>Time left on long timers: "6 d 23 h", "5 h 07 min", or a countdown under an hour.</summary>
        public static string TimeLeft(double seconds)
        {
            var total = (long)Math.Ceiling(Math.Max(0, seconds));
            if (total >= 86400)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} d {1} h", total / 86400, (total % 86400) / 3600);
            }

            if (total >= 3600)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} h {1:00} min", total / 3600, (total % 3600) / 60);
            }

            return Countdown(total);
        }

        /// <summary>A human duration: "30 segundos", "2 minutos", "1 h 05 min".</summary>
        public static string Duration(double seconds)
        {
            var total = (long)Math.Round(Math.Max(0, seconds));
            if (total < 60)
            {
                return total == 1 ? "1 segundo" : total + " segundos";
            }

            if (total < 3600)
            {
                var minutes = total / 60;
                var secs = total % 60;
                var head = minutes == 1 ? "1 minuto" : minutes + " minutos";
                return secs == 0 ? head : head + " e " + secs + " s";
            }

            var hours = total / 3600;
            var rest = (total % 3600) / 60;
            return string.Format(CultureInfo.InvariantCulture, "{0} h {1:00} min", hours, rest);
        }
    }
}
