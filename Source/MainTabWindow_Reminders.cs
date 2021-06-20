using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Reminders
{
    [StaticConstructorOnStartup]
    class MainTabWindow_Reminders : MainTabWindow
    {

        private static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete");
        private static readonly Texture2D RecurArrow = ContentFinder<Texture2D>.Get("UI/Recur");

        private readonly RemindersGameComponent Comp = Current.Game.GetComponent<RemindersGameComponent>();

        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(1000, 500);

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            var buttonRect = inRect;
            buttonRect.width = 300f;
            buttonRect.height = 30f;

            var newButtonPressed = Widgets.ButtonText(buttonRect, I18n.Translate("Main.New"));

            if (newButtonPressed)
            {
                Find.WindowStack.Add(new Window_EditReminder());
            }

            var remindersRect = inRect;

            remindersRect.yMin += 30;

            if (Comp.ReminderQueue.Count == 0)
            {
                Widgets.NoneLabel(remindersRect.y, remindersRect.width, I18n.Translate("Main.NoReminders"));
            }

            var yOffset = remindersRect.y;
            var index = 0;


            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;

            var nextLoadReminders = Comp.RemindersOnNextLoad.Take(200);
            var queuedReminders = Comp.ReminderQueue.Take(200);

            var totalHeight = (nextLoadReminders.Count() + queuedReminders.Count()) * 30f;

            var outRect = remindersRect;
            var viewRect = new Rect(0f, 0f, outRect.width / 2, totalHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            foreach (var reminder in Comp.RemindersOnNextLoad.Take(200))
            {
                var zebra = index++ % 2 == 0;
                DoReminderRow(new Rect(0f, yOffset, remindersRect.width - 5f, 30f), reminder, zebra, Comp.RemindersOnNextLoad);
                yOffset += 30f;
            }
            foreach (var reminder in Comp.ReminderQueue.Take(200))
            {
                var zebra = index++ % 2 == 0;
                DoReminderRow(new Rect(0f, yOffset, remindersRect.width - 5f, 30f), reminder, zebra, Comp.ReminderQueue.InnerList);
                yOffset += 30f;
            }
            Widgets.EndScrollView();

            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
        }

        private void DoReminderRow(Rect rect, Reminder reminder, bool zebra, List<Reminder> listToRemoveFrom)
        {
            if (zebra)
            {
                Widgets.DrawLightHighlight(rect);
            }
            Widgets.DrawHighlightIfMouseover(rect);

            var deleteRect = rect;
            deleteRect.width = 30f;


            var deletePressed = Widgets.ButtonImage(deleteRect, DeleteX);
            if (deletePressed)
            {
                Dialog_MessageBox confirmDelete = Dialog_MessageBox.CreateConfirmation(
                    I18n.Translate("ConfirmDelete.Body"),
                    () => listToRemoveFrom.Remove(reminder),
                    destructive: true,
                    I18n.Translate("ConfirmDelete.Title")
                );
                Find.WindowStack.Add(confirmDelete);
            }

            var iconRect = rect;
            iconRect.xMin += 35f;
            iconRect.width = 30f;

            var icon = reminder.LetterDef.Icon;

            if (icon != null)
            {
                var iconColor = reminder.LetterDef.color;
                GUI.color = iconColor;
                Widgets.DrawTextureFitted(iconRect, icon, .8f);
                if (reminder.RecurEvery.HasValue)
                {
                    Widgets.DrawTextureFitted(iconRect, RecurArrow, .5f);
                }
            }

            var fromNowRect = rect;
            fromNowRect.xMin = iconRect.xMax + 5f;
            fromNowRect.width = 80f;
            GUI.color = new Color(.75f, .75f, .75f);
            Widgets.Label(fromNowRect, reminder.FromNow.Truncate(fromNowRect.width));

            var titleRect = rect;
            titleRect.xMin = fromNowRect.xMax + 5f;
            titleRect.width = 300f;

            GUI.color = Color.white;
            Widgets.Label(titleRect, reminder.Title.Truncate(titleRect.width));

            var bodyRect = rect;
            bodyRect.xMin = titleRect.xMax + 5f;
            Widgets.Label(bodyRect, reminder.Body.Replace('\n', ' ').Truncate(bodyRect.width));

            var rowRect = rect;
            rowRect.xMin = deleteRect.xMax;
            if (Widgets.ButtonInvisible(rowRect))
            {
                Find.WindowStack.Add(new Window_EditReminder(reminder, reminder.FireOnTick == -1));
            }
        }

    }
}
