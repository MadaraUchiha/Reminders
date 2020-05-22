using Verse;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using System.Collections.Generic;

namespace Forgetful
{
    public class RemindersGameComponent : GameComponent
    {
        private const int DelayToSendOnLoadLetters = 10;
        public BetterFastPriorityQueue<Reminder> ReminderQueue = new BetterFastPriorityQueue<Reminder>();
        public List<Reminder> RemindersOnNextLoad = new List<Reminder>();
        public Game Game { get; private set; }

        private int ticksOnLoad = 0;

        public static Vector2? VectorForTime
        {
            get
            {
                if (WorldRendererUtility.WorldRenderedNow)
                {
                    if (Find.WorldSelector.selectedTile >= 0)
                    {
                        return Find.WorldGrid.LongLatOf(Find.WorldSelector.selectedTile);
                    }
                    if (Find.WorldSelector.NumSelectedObjects > 0)
                    {
                        return Find.WorldGrid.LongLatOf(Find.WorldSelector.FirstSelectedObject.Tile);
                    }
                }
                if (Find.CurrentMap == null)
                {
                    return null;
                }
                return Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile);
            }
        }

        public RemindersGameComponent(Game game)
        {
            Game = game;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Game.tickManager.TicksGame == ticksOnLoad + DelayToSendOnLoadLetters)
            {
                SendOnLoadLetters();
            }

            if (Game.tickManager.TicksGame % GenDate.TicksPerHour != 0) { return; }
            if (!VectorForTime.HasValue) { return; }
            if (ReminderQueue.Count == 0) { return; }

            Log.Debug(GenDate.DateFullStringWithHourAt(Game.tickManager.TicksAbs, VectorForTime.GetValueOrDefault()));
            while (ReminderQueue.Peek().FireOnTick <= GenTicks.TicksGame)
            {
                var reminder = ReminderQueue.Pop();
                SendLetter(reminder);
                if (ReminderQueue.Count == 0) { break; }
            }

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ticksOnLoad = Game.tickManager.TicksGame;
        }

        private void SendOnLoadLetters()
        {
            Log.Debug($"Sending {RemindersOnNextLoad.Count} letters on load");

            RemindersOnNextLoad.ForEach(r => SendLetter(r, true));

            RemindersOnNextLoad.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref ReminderQueue, nameof(ReminderQueue));
            Scribe_Collections.Look(ref RemindersOnNextLoad, nameof(RemindersOnNextLoad));
        }

        private void SendLetter(Reminder reminder, bool isNextLoad = false)
        {
            var scheduledFor = isNextLoad ? I18n.Translate("Reminder.NextLoad") : DateString(reminder.FireOnTick);
            Game.letterStack.ReceiveLetter(
                I18n.Translate("Title", reminder.Title),
                I18n.Translate("Body", scheduledFor, reminder.Title, reminder.Body),
                reminder.LetterDef
            );
        }

        private static string DateString(int ticks)
        {
            if (!VectorForTime.HasValue) { return "1st of January, 1970, 0h"; } // Kappa

            return GenDate.DateFullStringWithHourAt(GenDate.TickGameToAbs(ticks), VectorForTime.GetValueOrDefault());

        }

    }
}