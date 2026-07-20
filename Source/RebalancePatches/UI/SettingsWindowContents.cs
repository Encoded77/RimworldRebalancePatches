using System;
using System.Collections.Generic;
using System.Text;
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
        private const float ChevronSize = 18f;
        private const float ChildIndent = 26f;
        private const float LabelGap = 8f;
        private const float SectionGap = 4f;
        private const float SliderValueWidth = 36f;
        private const float SliderWidth = 200f;
        private const float SearchWidth = 260f;
        private const float NoteMaxWidth = 340f;

        private static Vector2 scrollPos;
        private static float lastHeight = 600f;

        private static string searchText = "";
        private static bool showInactive = true;
        private static readonly HashSet<string> expandedKeys = new HashSet<string>();

        private static readonly Color NoteColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color DisabledTint = new Color(1f, 1f, 1f, 0.4f);
        private static readonly Color MissingColor = new Color(1f, 0.6f, 0.35f);

        private static readonly Texture2D SliderRailAtlas = ContentFinder<Texture2D>.Get("UI/Buttons/SliderRail");
        private static readonly Texture2D SliderHandleTex = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle");

        private static readonly Dictionary<string, string> gateNoteCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> gateTooltipCache = new Dictionary<string, string>();

        private static string draggingKey;
        private static float lastDragSoundTime;

        public static void Draw(Rect inRect)
        {
            var topBar = new Rect(inRect.x, inRect.y, inRect.width, RowHeight);
            DrawTopBar(topBar);

            var banner = new Rect(inRect.x, topBar.yMax + 4f, inRect.width, 22f);
            Color prev = GUI.color;
            GUI.color = NoteColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(banner, "Changes take effect after restarting RimWorld. Greyed-out entries need mods that aren't loaded.");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            string query = searchText.Trim();
            bool searching = !query.NullOrEmpty();

            var body = new Rect(inRect.x, banner.yMax + 6f, inRect.width, inRect.height - (banner.yMax + 6f - inRect.y));
            var viewRect = new Rect(0f, 0f, body.width - 20f, lastHeight);
            Widgets.BeginScrollView(body, ref scrollPos, viewRect);
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(viewRect);
            try
            {
                bool anyOverhaulDrawn = false;
                bool dividerDrawn = false;
                foreach (RebalanceGroup group in SettingsRegistry.Groups)
                {
                    if (group.key == "dev" && !Prefs.DevMode)
                        continue;

                    bool groupEligible = ModEligibility.AllActive(group.requiredMods);
                    if (!groupEligible && !showInactive)
                        continue;

                    bool headerMatch = searching && MatchesQuery(query, group.label, null);
                    if (searching && !headerMatch && !AnyChildMatches(group, query))
                        continue;

                    if (!group.isOverhaul && anyOverhaulDrawn && !dividerDrawn)
                    {
                        listing.GapLine();
                        dividerDrawn = true;
                    }
                    anyOverhaulDrawn |= group.isOverhaul;

                    bool expanded = searching || expandedKeys.Contains(group.key);
                    DrawGroupHeader(listing.GetRect(RowHeight), group, groupEligible, expanded);

                    if (expanded)
                    {
                        bool groupOn = SettingsRegistry.GetEffective(group.key) && groupEligible;
                        foreach (RebalanceToggle child in group.children)
                        {
                            if (searching && !headerMatch && !MatchesQuery(query, child.label, child.description))
                                continue;
                            DrawChildRow(listing.GetRect(RowHeight), child, groupOn, groupEligible);
                        }
                        foreach (RebalanceSlider slider in group.sliders)
                        {
                            if (searching && !headerMatch && !MatchesQuery(query, slider.label, slider.description))
                                continue;
                            DrawSliderRow(listing.GetRect(RowHeight), slider, groupOn, groupEligible);
                        }
                    }
                    listing.Gap(SectionGap);
                }
            }
            finally
            {
                lastHeight = listing.CurHeight;
                listing.End();
                Widgets.EndScrollView();
            }
        }

        private static void DrawTopBar(Rect rect)
        {
            var searchRect = new Rect(rect.x, rect.y + 3f, SearchWidth, rect.height - 6f);
            searchText = Widgets.TextField(searchRect, searchText);
            if (searchText.NullOrEmpty() && !searchRect.Contains(Event.current.mousePosition))
            {
                Color prev = GUI.color;
                GUI.color = NoteColor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(searchRect.x + 6f, searchRect.y, searchRect.width - 6f, searchRect.height), "Search settings...");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = prev;
            }
            if (!searchText.NullOrEmpty())
            {
                var clearRect = new Rect(searchRect.xMax + 4f, rect.y + (rect.height - 18f) / 2f, 18f, 18f);
                if (Widgets.ButtonImage(clearRect, TexButton.CloseXSmall))
                {
                    searchText = "";
                    GUI.FocusControl(null);
                }
            }

            var collapseRect = new Rect(rect.xMax - 110f, rect.y + 2f, 110f, rect.height - 4f);
            bool anyExpanded = expandedKeys.Count > 0;
            if (Widgets.ButtonText(collapseRect, anyExpanded ? "Collapse all" : "Expand all"))
            {
                if (anyExpanded)
                {
                    expandedKeys.Clear();
                }
                else
                {
                    foreach (RebalanceGroup g in SettingsRegistry.Groups)
                        expandedKeys.Add(g.key);
                }
            }

            var inactiveRect = new Rect(collapseRect.x - LabelGap - 170f, rect.y, 170f, rect.height);
            Widgets.CheckboxLabeled(inactiveRect, "Show inactive mods", ref showInactive);
            TooltipHandler.TipRegion(inactiveRect, "Also list groups whose target mod isn't in the current modlist.");
        }

        private static void DrawGroupHeader(Rect rect, RebalanceGroup group, bool eligible, bool expanded)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            var chevronRect = new Rect(rect.x, rect.y + (rect.height - ChevronSize) / 2f, ChevronSize, ChevronSize);
            if (Widgets.ButtonImage(chevronRect, expanded ? TexButton.Collapse : TexButton.Reveal))
                ToggleExpanded(group.key);

            Color prev = GUI.color;
            if (!eligible)
                GUI.color = prev * DisabledTint;

            bool cur = SettingsRegistry.Own(group.key);
            bool v = cur && eligible;
            var box = new Vector2(chevronRect.xMax + 6f, rect.y + (rect.height - CheckboxSize) / 2f);
            Widgets.Checkbox(box, ref v, CheckboxSize, disabled: !eligible);

            float labelX = box.x + CheckboxSize + LabelGap;
            var labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - NoteMaxWidth, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, group.label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            var noteRect = new Rect(rect.xMax - NoteMaxWidth, rect.y, NoteMaxWidth, rect.height);
            if (!eligible)
            {
                string missing = ModEligibility.MissingNames(group.requiredMods);
                DrawNote(noteRect, "Not loaded: " + missing, MissingColor);
                TooltipHandler.TipRegion(rect, GateTooltip("group." + group.key, group.requiredMods, null,
                    "This mod isn't in the current modlist, so none of these patches can apply."));
            }
            else if (eligible && cur)
            {
                CountToggles(group, out int on, out int total);
                DrawNote(noteRect, on + "/" + total + " on", NoteColor);
            }
            else if (eligible)
            {
                DrawNote(noteRect, "group off", NoteColor);
            }

            if (Widgets.ButtonInvisible(labelRect))
                ToggleExpanded(group.key);
            if (eligible && v != cur)
                SettingsRegistry.Set(group.key, v);
        }

        private static void DrawChildRow(Rect rect, RebalanceToggle child, bool groupOn, bool groupEligible)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            bool modsOk = groupEligible && ModEligibility.AllActive(child.requiredMods) && ModEligibility.AnyActive(child.anyOfMods);
            bool depOn = child.dependsOn == null || SettingsRegistry.GetEffective(child.dependsOn);
            bool interactive = groupOn && modsOk && depOn;

            string tip = child.description;
            if (child.requiredMods.Length > 0 || child.anyOfMods.Length > 0)
                tip = GateTooltip(child.key, child.requiredMods, child.anyOfMods, child.description);
            if (!tip.NullOrEmpty())
                TooltipHandler.TipRegion(rect, tip);

            var box = new Vector2(rect.x + ChildIndent, rect.y + (rect.height - CheckboxSize) / 2f);
            float labelX = box.x + CheckboxSize + LabelGap;
            var labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - NoteMaxWidth, rect.height);

            Color prev = GUI.color;
            if (!interactive)
                GUI.color = prev * DisabledTint;

            bool cur = SettingsRegistry.Own(child.key);
            bool v = cur && modsOk;
            Widgets.Checkbox(box, ref v, CheckboxSize, disabled: !interactive);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, child.label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            var noteRect = new Rect(rect.xMax - NoteMaxWidth, rect.y, NoteMaxWidth, rect.height);
            if (groupEligible && !modsOk)
            {
                DrawNote(noteRect, GateNote(child.key, child.requiredMods, child.anyOfMods), MissingColor);
            }
            else if (modsOk && !depOn)
            {
                RebalanceToggle dep = SettingsRegistry.ToggleOf(child.dependsOn);
                DrawNote(noteRect, "Requires: " + (dep?.label ?? child.dependsOn), NoteColor);
            }

            if (interactive)
            {
                if (Widgets.ButtonInvisible(labelRect))
                    v = !v;
                if (v != cur)
                    SettingsRegistry.Set(child.key, v);
            }
        }

        private static void DrawSliderRow(Rect rect, RebalanceSlider slider, bool groupOn, bool groupEligible)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            bool modsOk = groupEligible && ModEligibility.AllActive(slider.requiredMods);
            string tip = slider.description + "\n\nDefault: " + slider.defaultValue;
            if (slider.requiredMods.Length > 0)
                tip = GateTooltip(slider.key, slider.requiredMods, null, slider.description + "\n\nDefault: " + slider.defaultValue);
            TooltipHandler.TipRegion(rect, tip);

            var box = new Vector2(rect.x + ChildIndent, rect.y + (rect.height - CheckboxSize) / 2f);
            float labelX = box.x + CheckboxSize + LabelGap;

            bool cur = SettingsRegistry.Own(slider.key);
            bool on = cur && modsOk;
            int curValue = SettingsRegistry.OwnValue(slider.key);
            bool interactive = groupOn && modsOk;
            bool rowOn = interactive && cur;

            Color prev = GUI.color;
            if (!rowOn)
                GUI.color = prev * DisabledTint;
            Widgets.Checkbox(box, ref on, CheckboxSize, disabled: !interactive);

            if (groupEligible && !modsOk)
            {
                var labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - NoteMaxWidth, rect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, slider.label);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = prev;
                DrawNote(new Rect(rect.xMax - NoteMaxWidth, rect.y, NoteMaxWidth, rect.height),
                    GateNote(slider.key, slider.requiredMods, null), MissingColor);
                return;
            }

            var revertRect = new Rect(rect.xMax - CheckboxSize, rect.y + (rect.height - CheckboxSize) / 2f,
                CheckboxSize, CheckboxSize);
            var valueRect = new Rect(revertRect.x - LabelGap - SliderValueWidth, rect.y, SliderValueWidth, rect.height);
            var sliderRect = new Rect(valueRect.x - LabelGap - SliderWidth, rect.y, SliderWidth, rect.height);
            var mainLabelRect = new Rect(labelX, rect.y, sliderRect.x - labelX - LabelGap, rect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(mainLabelRect, slider.label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(valueRect, curValue.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;

            if (interactive)
            {
                if (Widgets.ButtonInvisible(mainLabelRect))
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

        private static void ToggleExpanded(string key)
        {
            if (!expandedKeys.Add(key))
                expandedKeys.Remove(key);
        }

        private static bool MatchesQuery(string query, string label, string description)
        {
            if (label != null && label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return description != null && description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool AnyChildMatches(RebalanceGroup group, string query)
        {
            foreach (RebalanceToggle child in group.children)
                if (MatchesQuery(query, child.label, child.description))
                    return true;
            foreach (RebalanceSlider slider in group.sliders)
                if (MatchesQuery(query, slider.label, slider.description))
                    return true;
            return false;
        }

        private static void CountToggles(RebalanceGroup group, out int on, out int total)
        {
            on = 0;
            total = 0;
            foreach (RebalanceToggle child in group.children)
            {
                total++;
                bool modsOk = ModEligibility.AllActive(child.requiredMods) && ModEligibility.AnyActive(child.anyOfMods);
                if (modsOk && SettingsRegistry.Own(child.key))
                    on++;
            }
            foreach (RebalanceSlider slider in group.sliders)
            {
                total++;
                if (ModEligibility.AllActive(slider.requiredMods) && SettingsRegistry.Own(slider.key))
                    on++;
            }
        }

        private static void DrawNote(Rect rect, string text, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            Widgets.Label(rect, text.Truncate(rect.width));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = prev;
        }

        private static string GateNote(string cacheKey, string[] required, string[] anyOf)
        {
            if (gateNoteCache.TryGetValue(cacheKey, out string note))
                return note;

            var parts = new List<string>();
            string missing = ModEligibility.MissingNames(required);
            if (missing != null)
                parts.Add("Needs " + missing);
            if (anyOf != null && !ModEligibility.AnyActive(anyOf))
                parts.Add("Needs one of: " + ModEligibility.AllNames(anyOf));
            note = string.Join("; ", parts);
            gateNoteCache[cacheKey] = note;
            return note;
        }

        private static string GateTooltip(string cacheKey, string[] required, string[] anyOf, string description)
        {
            if (gateTooltipCache.TryGetValue(cacheKey, out string tip))
                return tip;

            var sb = new StringBuilder(description);
            if (required != null && required.Length > 0)
            {
                sb.Append("\n\nRequired mods:");
                foreach (string id in required)
                    sb.Append("\n  - ").Append(ModEligibility.NameOf(id))
                        .Append(ModEligibility.Active(id) ? " (loaded)" : " (missing)");
            }
            if (anyOf != null && anyOf.Length > 0)
            {
                sb.Append("\n\nNeeds at least one of:");
                foreach (string id in anyOf)
                    sb.Append("\n  - ").Append(ModEligibility.NameOf(id))
                        .Append(ModEligibility.Active(id) ? " (loaded)" : " (missing)");
            }
            tip = sb.ToString();
            gateTooltipCache[cacheKey] = tip;
            return tip;
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
