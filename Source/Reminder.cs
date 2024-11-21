using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Reminders
{
    public class Reminder: IExposable, IComparable
    {
        public LetterDef LetterDef = LetterDefOf.NeutralEvent;
        public string Title = "";
        public string Body = "";
        public int FireOnTick = int.MaxValue;

        public int? RecurEvery;

        public bool IsNextLoad { get => FireOnTick == -1; }

        public string FromNow
        {
            get
            {
                if (FireOnTick == int.MaxValue) { return I18n.Translate("Reminder.Never"); }
                if (FireOnTick == -1) { return I18n.Translate("Reminder.NextLoad"); }

                var difference = FireOnTick - Find.TickManager.TicksGame;

                return difference.ToStringTicksToPeriod();
            }
        }

        public string RecurEveryPeriod => RecurEvery?.ToStringTicksToPeriod();

        public static Reminder Recur(Reminder old)
        {
            return new Reminder()
            {
                Title = old.Title,
                Body = old.Body,
                LetterDef = old.LetterDef,
                RecurEvery = old.RecurEvery,
                FireOnTick = old.FireOnTick + (old.RecurEvery ?? 0)
            };
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
            Scribe_Values.Look(ref RecurEvery, nameof(RecurEvery));
        }
    }
}
