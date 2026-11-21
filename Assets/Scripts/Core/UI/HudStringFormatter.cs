using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// Pre-allocated char-buffer numeric formatting for the race HUD hot path (0 B/frame).
    /// All methods write into caller-owned char[] buffers and return the written length.
    /// No string allocations, no ToString(), no string.Format().
    /// </summary>
    public static class HudStringFormatter
    {
        /// <summary>Minimum buffer size for time formatting (MMM:SS.HH = up to 10 chars).</summary>
        public const int TimeBufferSize = 12;

        /// <summary>Minimum buffer size for integer formatting (up to 6 digits).</summary>
        public const int IntBufferSize = 8;

        /// <summary>Minimum buffer size for position formatting (e.g., "1st" = 4 chars).</summary>
        public const int PositionBufferSize = 8;

        /// <summary>
        /// Writes an integer (0–999999) into the buffer at the given offset.
        /// Returns the number of characters written. Negative values write "0".
        /// </summary>
        public static int WriteInt(char[] buffer, int offset, int value)
        {
            if (buffer == null || offset < 0 || offset >= buffer.Length)
            {
                return 0;
            }

            if (value < 0)
            {
                value = 0;
            }

            if (value == 0)
            {
                buffer[offset] = '0';
                return 1;
            }

            // Count digits
            int temp = value;
            int digits = 0;
            while (temp > 0)
            {
                digits++;
                temp /= 10;
            }

            int available = buffer.Length - offset;
            if (digits > available)
            {
                digits = available;
            }

            int end = offset + digits - 1;
            temp = value;
            for (int i = end; i >= offset; i--)
            {
                buffer[i] = (char)('0' + (temp % 10));
                temp /= 10;
            }

            return digits;
        }

        /// <summary>
        /// Writes a zero-padded integer with the specified width into the buffer.
        /// </summary>
        public static int WriteIntPadded(char[] buffer, int offset, int value, int width)
        {
            if (buffer == null || offset < 0 || width <= 0)
            {
                return 0;
            }

            int available = buffer.Length - offset;
            if (width > available)
            {
                width = available;
            }

            if (value < 0)
            {
                value = 0;
            }

            int end = offset + width - 1;
            int temp = value;
            for (int i = end; i >= offset; i--)
            {
                buffer[i] = (char)('0' + (temp % 10));
                temp /= 10;
            }

            return width;
        }

        /// <summary>
        /// Formats a race time (seconds) as "SS.HH" or "M:SS.HH" or "MM:SS.HH" into a buffer.
        /// Returns the number of characters written. 0 B allocation.
        /// </summary>
        public static int WriteRaceTime(char[] buffer, int offset, double totalSeconds)
        {
            if (buffer == null || offset < 0)
            {
                return 0;
            }

            if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds < 0d)
            {
                totalSeconds = 0d;
            }

            // Clamp to 999.99
            if (totalSeconds > 999.99d)
            {
                totalSeconds = 999.99d;
            }

            long totalHundredths = (long)Math.Floor((totalSeconds * 100d) + 0.5d);
            int hundredths = (int)(totalHundredths % 100L);
            int wholeSecs = (int)(totalHundredths / 100L);
            int minutes = wholeSecs / 60;
            int secs = wholeSecs % 60;

            int pos = offset;
            int available = buffer.Length - offset;

            if (minutes > 0 && available >= 7)
            {
                // Write minutes
                pos += WriteInt(buffer, pos, minutes);
                buffer[pos++] = ':';
                pos += WriteIntPadded(buffer, pos, secs, 2);
            }
            else if (available >= 4)
            {
                pos += WriteInt(buffer, pos, secs);
            }
            else
            {
                return 0;
            }

            if (pos < buffer.Length)
            {
                buffer[pos++] = '.';
            }

            if (pos + 1 < buffer.Length)
            {
                pos += WriteIntPadded(buffer, pos, hundredths, 2);
            }

            return pos - offset;
        }

        /// <summary>
        /// Formats a 1-based position with ordinal suffix (1st, 2nd, 3rd, 4th, 5th) into a buffer.
        /// Returns the number of characters written. 0 B allocation.
        /// </summary>
        public static int WritePosition(char[] buffer, int offset, int position)
        {
            if (buffer == null || offset < 0 || position < 1)
            {
                return 0;
            }

            int pos = offset;
            pos += WriteInt(buffer, pos, position);

            int available = buffer.Length - pos;
            if (available < 2)
            {
                return pos - offset;
            }

            // Ordinal suffix
            int lastTwo = position % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                buffer[pos++] = 't';
                buffer[pos++] = 'h';
            }
            else
            {
                int lastDigit = position % 10;
                switch (lastDigit)
                {
                    case 1:
                        buffer[pos++] = 's';
                        buffer[pos++] = 't';
                        break;
                    case 2:
                        buffer[pos++] = 'n';
                        buffer[pos++] = 'd';
                        break;
                    case 3:
                        buffer[pos++] = 'r';
                        buffer[pos++] = 'd';
                        break;
                    default:
                        buffer[pos++] = 't';
                        buffer[pos++] = 'h';
                        break;
                }
            }

            return pos - offset;
        }
    }
}
