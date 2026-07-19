using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RebalancePatches.UI
{
    [StaticConstructorOnStartup]
    public static class SettingsWindowContents
    {
        private const float RowHeight = 30f;
        private const float CheckboxSize = 24f;
        private const float ChildIndent = 26f;
        private const float LabelGap = 8f;
        private const float SectionGap = 12f;
        private const float SliderValueWidth = 36f;
        private const float SliderWidth = 200f;

        private static Vector2 scrollPos;
        private static float lastHeight = 600f;

        private static readonly Color NoteColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color DisabledTint = new Color(1f, 1f, 1f, 0.4f);

        private static readonly Texture2D SliderRailAtlas = ContentFinder<Texture2D>.Get("UI/Buttons/SliderRail");
        private static readonly Texture2D SliderHandleTex = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle");

        private static string draggingKey;
        private static float lastDragSoundTime;

        public static void Draw(Rect inRect)
        {
            var banner = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Color prev = GUI.color;
            GUI.color = NoteColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(banner, "Changes take effect after restarting RimWorld.");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            var body = new Rect(inRect.x, banner.yMax + 6f, inRect.width, inRect.height - banner.height - 6f);
            var viewRect = new Rect(0f, 0f, body.width - 20f, lastHeight);
            Widgets.BeginScrollView(body, ref scrollPos, viewRect);
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(viewRect);
            try
            {
                bool first = true;
                foreach (RebalanceGroup group in SettingsRegistry.Groups)
                {
                    if (group.key == "dev" && !Prefs.DevMode)
                        continue;
                    if (!first)
                        listing.GapLine(SectionGap);
                    first = false;

                    DrawGroupHeader(listing.GetRect(RowHeight), group);
                    bool groupOn = SettingsRegistry.GetEffective(group.key);
                    foreach (RebalanceToggle child in group.children)
                        DrawChildRow(listing.GetRect(RowHeight), child, groupOn);
                    foreach (RebalanceSlider slider in group.sliders)
                        DrawSliderRow(listing.GetRect(RowHeight), slider, groupOn);
                }
            }
            finally
            {
                lastHeight = listing.CurHeight;
                listing.End();
                Widgets.EndScrollView();
            }
        }

        private static void DrawGroupHeader(Rect rect, RebalanceGroup group)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            bool cur = SettingsRegistry.Own(group.key);
            bool v = cur;
            Widgets.Checkbox(new Vector2(rect.x, rect.y + (rect.height - CheckboxSize) / 2f), ref v, CheckboxSize);

            var labelRect = new Rect(rect.x + CheckboxSize + LabelGap, rect.y,
                rect.width - CheckboxSize - LabelGap, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, group.label);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonInvisible(labelRect))
                v = !v;
            if (v != cur)
                SettingsRegistry.Set(group.key, v);
        }

        private static void DrawChildRow(Rect rect, RebalanceToggle child, bool groupOn)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            if (!child.description.NullOrEmpty())
                TooltipHandler.TipRegion(rect, child.description);

            var box = new Vector2(rect.x + ChildIndent, rect.y + (rect.height - CheckboxSize) / 2f);
            var labelRect = new Rect(box.x + CheckboxSize + LabelGap, rect.y,
                rect.width - ChildIndent - CheckboxSize - LabelGap, rect.height);

            Color prev = GUI.color;
            if (!groupOn)
                GUI.color = prev * DisabledTint;

            bool cur = SettingsRegistry.Own(child.key);
            bool v = cur;
            Widgets.Checkbox(box, ref v, CheckboxSize, disabled: !groupOn);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, child.label);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.color = prev;

            if (groupOn)
            {
                if (Widgets.ButtonInvisible(labelRect))
                    v = !v;
                if (v != cur)
                    SettingsRegistry.Set(child.key, v);
            }
        }

        private static void DrawSliderRow(Rect rect, RebalanceSlider slider, bool groupOn)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            if (!slider.description.NullOrEmpty())
                TooltipHandler.TipRegion(rect, slider.description + "\n\nDefault: " + slider.defaultValue);

            var box = new Vector2(rect.x + ChildIndent, rect.y + (rect.height - CheckboxSize) / 2f);
            var revertRect = new Rect(rect.xMax - CheckboxSize, rect.y + (rect.height - CheckboxSize) / 2f,
                CheckboxSize, CheckboxSize);
            var valueRect = new Rect(revertRect.x - LabelGap - SliderValueWidth, rect.y, SliderValueWidth, rect.height);
            var sliderRect = new Rect(valueRect.x - LabelGap - SliderWidth, rect.y, SliderWidth, rect.height);
            var labelRect = new Rect(box.x + CheckboxSize + LabelGap, rect.y,
                sliderRect.x - box.x - CheckboxSize - 2f * LabelGap, rect.height);

            bool cur = SettingsRegistry.Own(slider.key);
            bool on = cur;
            int curValue = SettingsRegistry.OwnValue(slider.key);
            bool rowOn = groupOn && cur;

            Color prev = GUI.color;
            if (!rowOn)
                GUI.color = prev * DisabledTint;
            Widgets.Checkbox(box, ref on, CheckboxSize, disabled: !groupOn);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, slider.label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(valueRect, curValue.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            if (groupOn)
            {
                if (Widgets.ButtonInvisible(labelRect))
                    on = !on;
                if (on != cur)
                    SettingsRegistry.Set(slider.key, on);
            }

            if (rowOn)
            {
                float raw = Slider(sliderRect, slider.key, curValue, slider.min, slider.max);
                int v = Mathf.RoundToInt(raw);
                if (v != curValue)
                    SettingsRegistry.SetValue(slider.key, v);

                if (SettingsRegistry.IsValueOverridden(slider.key))
                {
                    TooltipHandler.TipRegion(revertRect, "Reset to default (" + slider.defaultValue + ")");
                    if (Widgets.ButtonImage(revertRect, TexButton.Reload))
                        SettingsRegistry.ClearValue(slider.key);
                }
            }
            else
            {
                DrawInertSlider(sliderRect, curValue, slider.min, slider.max);
            }
        }

        private static float Slider(Rect cell, string dragKey, float value, float min, float max)
        {
            float centerY = cell.y + cell.height / 2f;
            var rail = new Rect(cell.x + 6f, centerY - 4f, cell.width - 12f, 8f);
            Widgets.DrawAtlas(rail, SliderRailAtlas);
            float t = max > min ? Mathf.InverseLerp(min, max, value) : 0f;
            float handleCenter = Mathf.Lerp(rail.x, rail.xMax, t);
            GUI.DrawTexture(new Rect(handleCenter - 6f, centerY - 6f, 12f, 12f), SliderHandleTex);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(cell))
            {
                draggingKey = dragKey;
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                Event.current.Use();
            }

            if (draggingKey == dragKey)
            {
                if (Event.current.rawType == EventType.MouseUp)
                {
                    draggingKey = null;
                }
                else if (UnityGUIBugsFixer.MouseDrag())
                {
                    float dragged = Mathf.Clamp(
                        (Event.current.mousePosition.x - rail.x) / rail.width * (max - min) + min, min, max);
                    if (Event.current.type == EventType.MouseDrag)
                        Event.current.Use();
                    if (dragged != value && Time.realtimeSinceStartup > lastDragSoundTime + 0.075f)
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        lastDragSoundTime = Time.realtimeSinceStartup;
                    }
                    value = dragged;
                }
            }

            return value;
        }

        private static void DrawInertSlider(Rect rect, float value, float min, float max)
        {
            Color prev = GUI.color;
            GUI.color = prev * DisabledTint;
            float centerY = rect.y + rect.height / 2f;
            Widgets.DrawLineHorizontal(rect.x + 6f, centerY, rect.width - 12f);
            float t = max > min ? Mathf.InverseLerp(min, max, value) : 0f;
            float handleX = Mathf.Lerp(rect.x + 6f, rect.xMax - 6f, t);
            Widgets.DrawBoxSolid(new Rect(handleX - 3f, centerY - 5f, 6f, 10f), new Color(0.7f, 0.7f, 0.7f, 0.6f));
            GUI.color = prev;
        }
    }
}
