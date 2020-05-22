using RimWorld;
using Verse;

namespace Forgetful
{
    public static class DateHelper
    {
        public static int DateToGameTicksOffsetAgnostic(int year, Quadrum quadrum, int day, int hour)
        {
            var yearDiff = year - GenDate.DefaultStartingYear;
            var yearTicks = yearDiff * GenDate.TicksPerYear;

            var quadrumTicks = (int)quadrum * GenDate.TicksPerQuadrum;

            // day is 1-15, map to 0-14
            var dayTicks = (day - 1) * GenDate.TicksPerDay;

            var hourTicks = hour * GenDate.TicksPerHour;

            return GenDate.TickAbsToGame(yearTicks + quadrumTicks + dayTicks + hourTicks);
        }

        public static int RelativeTimeToGameTicks(int days, int hours)
        {
            var periodTicks = days * GenDate.TicksPerDay + hours * GenDate.TicksPerHour;
            return ToNearestHour(GenDate.TickAbsToGame(periodTicks + Find.TickManager.TicksAbs));
        }

        public static int ToNearestHour(int ticksAbs)
        {
            var remainder = ticksAbs % GenDate.TicksPerHour;
            return ticksAbs - remainder;
        }
    }
}
