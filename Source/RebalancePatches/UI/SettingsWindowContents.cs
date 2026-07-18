using UnityEngine;
using Verse;

namespace RebalancePatches.UI
{
    public static class SettingsWindowContents
    {
        private const float RowHeight = 30f;
        private const float CheckboxSize = 24f;
        private const float ChildIndent = 26f;
        private const float LabelGap = 8f;
        private const float SectionGap = 12f;

        private static Vector2 scrollPos;
        private static float lastHeight = 600f;

        private static readonly Color NoteColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color DisabledTint = new Color(1f, 1f, 1f, 0.4f);

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
                    if (!first)
                        listing.GapLine(SectionGap);
                    first = false;

                    DrawGroupHeader(listing.GetRect(RowHeight), group);
                    bool groupOn = SettingsRegistry.GetEffective(group.key);
                    foreach (RebalanceToggle child in group.children)
                        DrawChildRow(listing.GetRect(RowHeight), child, groupOn);
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
    }
}
