using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Forgetful
{
    public static partial class FWidgets
    {
        private static readonly Color DefaultColor = Color.white;


        public static void Label(Rect canvas, string label, Color? color = null,
            TextAnchor anchor = TextAnchor.UpperLeft, GameFont font = GameFont.Small)
        {
            var _color = GUI.color;
            var _anchor = Text.Anchor;
            var _font = Text.Font;
            GUI.color = color ?? DefaultColor;
            Text.Anchor = anchor;
            Text.Font = font;
            Verse.Widgets.Label(canvas, label);
            Text.Font = _font;
            Text.Anchor = _anchor;
            GUI.color = _color;
        }

        public static bool RadioButtonLabeled(Rect rect, string labelText, bool chosen)
        {
            var pressed = Widgets.ButtonInvisible(rect);
            if (pressed && !chosen)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }

            Widgets.RadioButton(rect.x, rect.y, chosen);

            var labelRect = rect;
            labelRect.xMin += 24f + 5f; // 24 is the radio button size, 5 is margin
            Label(labelRect, labelText, anchor: TextAnchor.MiddleLeft);

            return pressed;
        }


    }

}