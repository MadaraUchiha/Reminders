using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Forgetful
{
    public class Reminder: IExposable, IComparable
    {
        public LetterDef LetterDef = LetterDefOf.NeutralEvent;
        public string Title = "";
        public string Body = "";
        public int FireOnTick = int.MaxValue;

        public string FromNow
        {
            get
            {
                if (FireOnTick == int.MaxValue) { return I18n.Translate("Reminder.Never"); }
                if (FireOnTick == -1) { return I18n.Translate("Reminder.NextLoad"); }

                var difference = GenDate.TickGameToAbs(FireOnTick) - Find.TickManager.TicksAbs;

                return difference.ToStringTicksToPeriod();
            }
        }


        public int CompareTo(object obj)
        {
            if (obj == null) { return 1; }
            if (obj is Reminder other)
            {
                return FireOnTick.CompareTo(other.FireOnTick);
            }
            throw new ArgumentException($"Tried to compare {nameof(Reminder)} with another object that was not a {nameof(Reminder)}");
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref LetterDef, nameof(LetterDef));
            Scribe_Values.Look(ref Title, nameof(Title));
            Scribe_Values.Look(ref Body, nameof(Body));
            Scribe_Values.Look(ref FireOnTick, nameof(FireOnTick));
        }
    }
}
