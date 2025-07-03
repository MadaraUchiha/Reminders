using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Reminders
{
    class Window_EditReminder : Window
    {
        private static readonly Dictionary<LetterDef, string> letterOptions = new Dictionary<LetterDef, string>() {
            { LetterDefOf.ThreatBig, I18n.Translate("Reminder.ThreatBig") },
            { LetterDefOf.ThreatSmall, I18n.Translate("Reminder.ThreatSmall") },
            { LetterDefOf.NegativeEvent, I18n.Translate("Reminder.Negative") },
            { LetterDefOf.NeutralEvent, I18n.Translate("Reminder.Neutral") },
            { LetterDefOf.PositiveEvent, I18n.Translate("Reminder.Positive") },
        };

        private readonly Reminder reminder;

        private string title = "";
        private string body = "";
        private LetterDef selectedLetterDef = LetterDefOf.NeutralEvent;
        private int fireOnTick;
        private bool nextLoad;
        private Vector2 bodyTextAreaScrollPosition;

        private TimeRepresentation selectedTimeRep = TimeRepresentation.Absolute;

        private readonly float absTextLength = Text.CalcSize(I18n.Translate(TimeRepresentation.Absolute.ToString())).x;
        private readonly float relTextLength = Text.CalcSize(I18n.Translate(TimeRepresentation.Relative.ToString())).x;

        private int dayValue;
        private string dayBuffer;
        private Quadrum quadrumValue;
        private int yearValue;
        private string yearBuffer;
        private int hourValue;
        private string hourBuffer;

        private int daysFromNowValue = 0;
        private string daysFromNowBuffer;
        private int hoursFromNowValue = 8;
        private string hoursFromNowBuffer;

        private bool recur;
        private int recurEveryDays = 0;
        private string recurEveryDaysBuffer;
        private int recurEveryHours = 8;
        private string recurEveryHoursBuffer;

        private const int DefaultPeriod = 8 * GenDate.TicksPerHour;

        public Window_EditReminder()
        {
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnCancel = true;
            closeOnAccept = false;
        }

        public Window_EditReminder(Reminder reminder, bool fromNextLoadQueue) : this()
        {
            this.reminder = reminder;
            title = reminder.Title;
            body = reminder.Body;
            selectedLetterDef = reminder.LetterDef;
            fireOnTick = fromNextLoadQueue ? 0 : reminder.FireOnTick;
            nextLoad = fromNextLoadQueue;
            if (nextLoad)
            {
                selectedTimeRep = TimeRepresentation.NextLoad;
            }
            if (reminder.RecurEvery.HasValue)
            {
                recur = true;
                var days = Mathf.FloorToInt((float)reminder.RecurEvery.Value / (float)GenDate.TicksPerDay);
                var ticksRemainder = reminder.RecurEvery.Value - days * GenDate.TicksPerDay;
                var hours = Mathf.CeilToInt((float)ticksRemainder / (float)GenDate.TicksPerHour);
                recurEveryDays = days;
                recurEveryHours = hours;
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (fireOnTick == 0 || nextLoad)
            {
                fireOnTick = Find.TickManager.TicksGame + DefaultPeriod;
            }


            UpdateAbsoluteFieldsFromTicks(fireOnTick);
            UpdateRelativeFieldsFromTicks(fireOnTick);
        }

        private void UpdateAbsoluteFieldsFromTicks(int ticksToUse)
        {
            var tileCoords = RemindersGameComponent.VectorForTime;
            if (!tileCoords.HasValue) { return; }
            var absTicks = GenDate.TickGameToAbs(ticksToUse);
            var coords = tileCoords.Value;
            yearValue = GenDate.Year(absTicks, coords.x);
            yearBuffer = yearValue.ToString();

            quadrumValue = GenDate.Quadrum(absTicks, coords.x);

            dayValue = GenDate.DayOfQuadrum(absTicks, coords.x) + 1;
            dayBuffer = dayValue.ToString();

            hourValue = GenDate.HourOfDay(absTicks, coords.x);
            hourBuffer = hourValue.ToString();

            Log.Debug($"Set defaults to {dayBuffer} {quadrumValue}, {yearBuffer} at {hourBuffer}.");
            Log.Debug($"Should have set to {GenDate.DateFullStringWithHourAt(absTicks, coords)}.");
        }

        private void UpdateRelativeFieldsFromTicks(int ticksToUse)
        {
            var diff = GenDate.TickGameToAbs(ticksToUse) - Find.TickManager.TicksAbs;
            int years = 0, quadrums = 0, days = 0;
            var hours = 8f;
            if (diff > 0)
            {
                GenDate.TicksToPeriod(diff, out years, out quadrums, out days, out hours);
            }

            hoursFromNowValue = Mathf.CeilToInt(hours);
            daysFromNowValue = days + quadrums * GenDate.DaysPerQuadrum + years * GenDate.DaysPerYear;

            var remainder = hoursFromNowValue == 24;
            if (remainder)
            {
                hoursFromNowValue = 0;
                daysFromNowValue += 1;
            }

            hoursFromNowBuffer = hoursFromNowValue.ToString();
            daysFromNowBuffer = daysFromNowValue.ToString();

            Log.Debug($"Set to {hoursFromNowValue} ({hoursFromNowBuffer}) on day {daysFromNowValue} ({daysFromNowBuffer})");
        }

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            FWidgets.Label(inRect, I18n.Translate("EditReminder.Title"), font: GameFont.Medium);
            inRect.yMin += 40;

            var selectRect = inRect;
            selectRect.height = 60;
            DoLetterDefSelection(selectRect);

            var titleRect = selectRect;
            titleRect.y = selectRect.yMax + 15;
            titleRect.height = 55;
            DoTitle(titleRect);

            var bodyRect = titleRect;
            bodyRect.y = titleRect.yMax + 15;
            bodyRect.height = 85;
            DoBody(bodyRect);

            var timeRect = bodyRect;
            timeRect.y = bodyRect.yMax + 15;
            DoTime(timeRect);

            if (selectedTimeRep != TimeRepresentation.NextLoad)
            {
                var recurRect = timeRect;
                recurRect.y = timeRect.yMax + 15;
                recurRect.height = 30;
                DoRecurring(recurRect);
            }
            else
            {
                recur = false;
            }

            var buttonsRect = inRect;
            buttonsRect.yMin = inRect.yMax - 50;
            buttonsRect.xMin = inRect.xMax - 210; // 100 per button * 2, + 10 margin
            DoButtons(buttonsRect);
        }

        private void DoLetterDefSelection(Rect rect)
        {
            var labelRect = rect;
            labelRect.height = 25;
            Widgets.Label(labelRect, I18n.Translate("EditReminder.Urgency"));

            var buttonRect = labelRect;
            buttonRect.width = 140;
            buttonRect.height = 30;
            buttonRect.y = labelRect.yMax + 5;
            var letterText = I18n.Translate("EditReminder.SelectUrgency");
            var buttonPressed = Widgets.ButtonText(buttonRect, letterText);

            if (buttonPressed)
            {
                Find.WindowStack.Add(new FloatMenu(LetterOptions()));
            }

            var iconRect = new Rect(buttonRect.xMax + 30, buttonRect.y, 30f, buttonRect.height);
            GUI.color = selectedLetterDef.color;
            Widgets.DrawTextureFitted(iconRect, selectedLetterDef.Icon, .9f);
            GUI.color = Color.white;

            var selectionLabelRect = iconRect;
            selectionLabelRect.x = iconRect.xMax + 10;
            selectionLabelRect.xMax = rect.xMax;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(selectionLabelRect, letterOptions[selectedLetterDef]);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private List<FloatMenuOption> LetterOptions()
        {
            return letterOptions
                .Select(entry =>
                    new FloatMenuOption(
                        label: entry.Value,
                        action: () => selectedLetterDef = entry.Key,
                        iconTex: entry.Key.Icon,
                        iconColor: entry.Key.color,
                        extraPartWidth: 29f
                    ))
                .ToList();
        }

        private void DoTitle(Rect rect)
        {
            var labelRect = rect;
            labelRect.height = 25;

            Widgets.Label(labelRect, I18n.Translate("EditReminder.ReminderTitle"));

            var inputRect = labelRect;
            inputRect.y = labelRect.yMax;
            inputRect.height = 30;
            inputRect.width -= 16f; // Align with textarea below. 16f is reserved for scrollbar.

            title = Widgets.TextField(inputRect, title);
        }

        private void DoBody(Rect rect)
        {
            var labelRect = rect;
            labelRect.height = 25;

            Widgets.Label(labelRect, I18n.Translate("EditReminder.ReminderBody"));

            var inputRect = labelRect;
            inputRect.y = labelRect.yMax;
            inputRect.height = 60;

            body = TextAreaScrollable(inputRect, body, ref bodyTextAreaScrollPosition);
        }

        private string TextAreaScrollable(Rect rect, string text, ref Vector2 scrollbarPosition, bool readOnly = false)
        {
            Rect rect2 = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(Text.CalcHeight(text, rect.width) + 10f, rect.height));
            Widgets.BeginScrollView(rect, ref scrollbarPosition, rect2, true);
            string result = Widgets.TextArea(rect2, text, readOnly);
            Widgets.EndScrollView();
            return result;
        }

        private void DoTime(Rect rect)
        {
            var labelRect = rect;
            labelRect.height = 25;
            Widgets.Label(labelRect, I18n.Translate("EditReminder.ReminderTime"));

            var radioRect = labelRect;
            radioRect.y = labelRect.yMax;
            radioRect.height = 30;
            radioRect.width = 24f + 20f + absTextLength;

            if (FWidgets.RadioButtonLabeled(radioRect, I18n.Translate(TimeRepresentation.Absolute.ToString()), selectedTimeRep == TimeRepresentation.Absolute))
            {
                if (selectedTimeRep != TimeRepresentation.Absolute && fireOnTick != 0)
                {
                    UpdateAbsoluteFieldsFromTicks(fireOnTick);
                }
                selectedTimeRep = TimeRepresentation.Absolute;
            }

            radioRect.x = radioRect.xMax + 20f;
            radioRect.width = 24f + 20f + relTextLength;

            if (FWidgets.RadioButtonLabeled(radioRect, I18n.Translate(TimeRepresentation.Relative.ToString()), selectedTimeRep == TimeRepresentation.Relative))
            {
                if (selectedTimeRep != TimeRepresentation.Relative && fireOnTick != 0)
                {
                    UpdateRelativeFieldsFromTicks(fireOnTick);
                }
                selectedTimeRep = TimeRepresentation.Relative;
            }

            radioRect.x = radioRect.xMax + 20f;
            radioRect.width = 100f;
            bool nextLoadRadioSelected = FWidgets.RadioButtonLabeled(radioRect, I18n.Translate("Reminder.NextLoad"), selectedTimeRep == TimeRepresentation.NextLoad);
            if (nextLoadRadioSelected)
            {
                selectedTimeRep = TimeRepresentation.NextLoad;
            }
            nextLoad = selectedTimeRep == TimeRepresentation.NextLoad;

            var timeInputRect = new Rect(labelRect.x, radioRect.yMax + 5, rect.width, 30f);

            switch (selectedTimeRep)
            {
                case TimeRepresentation.Absolute:
                    DoAbsoluteTime(timeInputRect);
                    break;
                case TimeRepresentation.Relative:
                    DoRelativeTime(timeInputRect);
                    break;
            }
        }

        private void DoAbsoluteTime(Rect rect)
        {
            FWidgets.Label(rect, I18n.Translate("EditReminder.FireOn"), anchor: TextAnchor.MiddleLeft);

            var dayRect = rect;
            dayRect.x = rect.x + 60f;
            dayRect.width = 30f;
            Widgets.TextFieldNumeric(dayRect, ref dayValue, ref dayBuffer, 1, 15);

            var ofRect = dayRect;
            ofRect.x = dayRect.xMax + 4f;
            ofRect.width = 15f;
            FWidgets.Label(ofRect, I18n.Translate("Of"), anchor: TextAnchor.MiddleCenter);

            var quadrumRect = ofRect;
            quadrumRect.x = ofRect.xMax + 4f;
            quadrumRect.width = 100f;
            var pressed = Widgets.ButtonText(quadrumRect, quadrumValue.Label());
            if (pressed)
            {
                Find.WindowStack.Add(new FloatMenu(MakeQuadrumOptions()));
            }

            var commaRect = ofRect;
            commaRect.x = quadrumRect.xMax + 2f;
            commaRect.width = 5f;
            FWidgets.Label(commaRect, ",", anchor: TextAnchor.MiddleCenter);

            var yearRect = commaRect;
            yearRect.x = commaRect.xMax + 4f;
            yearRect.width = 60f;
            Widgets.TextFieldNumeric(yearRect, ref yearValue, ref yearBuffer);

            var atRect = commaRect;
            atRect.x = yearRect.xMax + 4f;
            atRect.width = 15f;
            FWidgets.Label(atRect, I18n.Translate("At"), anchor: TextAnchor.MiddleCenter);

            var hourRect = atRect;
            hourRect.x = atRect.xMax + 4f;
            hourRect.width = 30f;
            Widgets.TextFieldNumeric(hourRect, ref hourValue, ref hourBuffer, 0, 23);

            var offsetHours = RemindersGameComponent.VectorForTime.HasValue ? GenDate.TimeZoneAt(RemindersGameComponent.VectorForTime.Value.x) : 0;

            int dateToTicks = DateHelper.DateToGameTicksOffsetAgnostic(yearValue, quadrumValue, dayValue, hourValue, offsetHours);
            fireOnTick = dateToTicks;
        }

        private List<FloatMenuOption> MakeQuadrumOptions()
        {
            return Enum.GetValues(typeof(Quadrum)).Cast<Quadrum>()
                .Where(q => q != Quadrum.Undefined)
                .Select(q => new FloatMenuOption(q.Label(), () => quadrumValue = q))
                .ToList();
        }

        private void DoRelativeTime(Rect rect)
        {
            FWidgets.Label(rect, I18n.Translate("EditReminder.FireIn"), anchor: TextAnchor.MiddleLeft);

            var dayRect = rect;
            dayRect.x += 60f;
            dayRect.width = 60f;
            Widgets.TextFieldNumeric(dayRect, ref daysFromNowValue, ref daysFromNowBuffer);

            var dayLabelRect = dayRect;
            dayLabelRect.x = dayRect.xMax + 4f;
            FWidgets.Label(dayLabelRect, I18n.Translate("EditReminder.DaysAnd"), anchor: TextAnchor.MiddleCenter);

            var hourRect = dayLabelRect;
            hourRect.x = dayLabelRect.xMax + 4f;
            hourRect.width = 30f;
            Widgets.TextFieldNumeric(hourRect, ref hoursFromNowValue, ref hoursFromNowBuffer, 0, 23);

            var hourLabelRect = hourRect;
            hourLabelRect.x = hourRect.xMax + 4f;
            hourLabelRect.width = 60f;
            FWidgets.Label(hourLabelRect, I18n.Translate("EditReminder.Hours"), anchor: TextAnchor.MiddleLeft);

            fireOnTick = DateHelper.RelativeTimeToGameTicks(daysFromNowValue, hoursFromNowValue);
        }

        private void DoRecurring(Rect rect)
        {

            Widgets.CheckboxLabeled(rect, I18n.Translate("EditReminder.Recur"), ref recur);
            if (recur)
            {
                var timeInputRect = rect;
                timeInputRect.y = rect.yMax + 5;
                timeInputRect.height = 30;
                FWidgets.Label(timeInputRect, I18n.Translate("EditReminder.RecurEvery"), anchor: TextAnchor.MiddleLeft);

                var dayRect = timeInputRect;
                dayRect.x += 120f;
                dayRect.width = 60f;
                Widgets.TextFieldNumeric(dayRect, ref recurEveryDays, ref recurEveryDaysBuffer);

                var dayLabelRect = dayRect;
                dayLabelRect.x = dayRect.xMax + 4f;
                FWidgets.Label(dayLabelRect, I18n.Translate("EditReminder.DaysAnd"), anchor: TextAnchor.MiddleCenter);

                var hourRect = dayLabelRect;
                hourRect.x = dayLabelRect.xMax + 4f;
                hourRect.width = 30f;
                Widgets.TextFieldNumeric(hourRect, ref recurEveryHours, ref recurEveryHoursBuffer, 0, 23);

                var hourLabelRect = hourRect;
                hourLabelRect.x = hourRect.xMax + 4f;
                hourLabelRect.width = 60f;
                FWidgets.Label(hourLabelRect, I18n.Translate("EditReminder.Hours"), anchor: TextAnchor.MiddleLeft);
            }

        }

        private void DoButtons(Rect rect)
        {
            var margin = 10;
            var buttonWidth = (rect.width - 10) / 2;

            var discardRect = rect;
            discardRect.width = buttonWidth;

            var _color = GUI.color;
            GUI.color = new Color(1f, .5f, .5f);
            if (Widgets.ButtonText(discardRect, I18n.Translate("EditReminder.Discard")))
            {
                Close();
            }

            var saveRect = discardRect;
            saveRect.x = discardRect.xMax + margin;

            GUI.color = new Color(.5f, 1f, .5f);
            if (Widgets.ButtonText(saveRect, I18n.Translate("EditReminder.Save")))
            {
                SaveAndClose();
            }

            GUI.color = _color;
        }

        private void SaveAndClose()
        {
            if (!Validate())
            {
                return;
            }

            var comp = Current.Game.GetComponent<RemindersGameComponent>();
            if (reminder != null)
            {
                comp.ReminderQueue.InnerList.Remove(reminder);
                comp.RemindersOnNextLoad.Remove(reminder);
            }

            var recurDuration = recurEveryDays * GenDate.TicksPerDay + recurEveryHours * GenDate.TicksPerHour;

            var newReminder = new Reminder()
            {
                Title = title,
                Body = body,
                FireOnTick = nextLoad ? -1 : fireOnTick,
                LetterDef = selectedLetterDef,
                RecurEvery = recur ? (int?)recurDuration : null
            };

            if (nextLoad)
            {
                comp.RemindersOnNextLoad.Add(newReminder);
            }
            else
            {
                comp.ReminderQueue.Push(newReminder);
            }

            Close();
        }

        private bool Validate()
        {
            if (title.NullOrEmpty())
            {
                Messages.Message(I18n.Translate("EditReminder.ErorrEmptyTitle"), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (fireOnTick < Find.TickManager.TicksGame)
            {
                Messages.Message(I18n.Translate("EditReminder.ErrorPast"), MessageTypeDefOf.RejectInput);
                return false;
            }
            var recurDuration = recurEveryDays * GenDate.TicksPerDay + recurEveryHours * GenDate.TicksPerHour;
            if (recur && recurDuration == 0)
            {
                Messages.Message(I18n.Translate("EditReminder.ErrorInvalidRecur"), MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        private enum TimeRepresentation
        {
            Absolute,
            Relative,
            NextLoad,
        }

        private enum Period
        {
            Hours = GenDate.TicksPerHour,
            Days = GenDate.TicksPerDay,
            Quadrums = GenDate.TicksPerQuadrum,
            Years = GenDate.TicksPerYear,
        }
    }
}
