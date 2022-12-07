using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EnhancedGrowthVatLearning;

internal static class ListingHelper
{
    /// <summary>
    ///     Wrapper for setting up a basic scroll view. The returned Listing_Standard is limited to one column
    ///     to make it possible to set up a dynamic height for the content rect. If multiple columns are required,
    ///     create an inner listing and use that for layout.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="viewRect"></param>
    /// <param name="contentHeight"></param>
    /// <param name="scrollPosition"></param>
    /// <returns></returns>
    public static Listing_Standard BeginScrollView(this Listing_Standard list, Rect viewRect, float contentHeight, ref Vector2 scrollPosition)
    {
        //set up scrolling environment
        Rect innerContentRect = new(0f, 0f, viewRect.width - 20f, contentHeight);
        Rect viewBoundaryRect = new(0f, 0f, viewRect.width - 20f, viewRect.height);
        if (contentHeight <= viewRect.height)
        {
            innerContentRect.width = viewRect.width;
            viewBoundaryRect.width = viewRect.width;
        }

        Widgets.BeginScrollView(viewRect, ref scrollPosition, innerContentRect);

        Vector2 position = scrollPosition;
        //the content listing can't wrap, make an inner listing if you want that.
        Listing_Standard scrollList = new(viewBoundaryRect, () => position)
        {
            maxOneColumn = true
        };

        scrollList.Begin(innerContentRect);

        return scrollList;
    }

    public static void EndScrollView(this Listing_Standard list, Listing_Standard scrollList)
    {
        scrollList.End();
        Widgets.EndScrollView();
    }

    public static void CheckboxLabeled(this Listing_Standard list, string label, ref bool checkOn, string tooltip, bool tooltipCoversButtonOnly)
    {
        if (tooltipCoversButtonOnly)
        {
            Rect fullRect = list.GetRect(Text.LineHeight);
            Widgets.Label(fullRect.LeftPart(0.9f), label);

            Rect rect = fullRect.RightPartPixels(24f); //all we need for the checkbox
            if (!list.BoundingRectCached.HasValue || rect.Overlaps(list.BoundingRectCached.Value))
            {
                if (!tooltip.NullOrEmpty())
                {
                    if (Mouse.IsOver(rect))
                        Widgets.DrawHighlight(rect);

                    TooltipHandler.TipRegion(rect, (TipSignal)tooltip);
                }

                Widgets.Checkbox(rect.x, rect.y, ref checkOn);
            }

            list.Gap(list.verticalSpacing);
        }
        else
        {
            list.CheckboxLabeled(label, ref checkOn, tooltip);
        }
    }

    internal static void TextFieldNumericLabeledTooltip<T>(this Listing_Standard list, string settingsLabel, ref T val, string tipSignal, ref string buffer, float min, float max)
        where T : struct
    {
        Rect rect = list.GetRect(Text.LineHeight);
        Widgets.TextFieldNumericLabeled(rect, $"{settingsLabel} ", ref val, ref buffer, min, max);
        TooltipHandler.TipRegion(rect, tipSignal);
    }


    internal static Rect SliderWithTextField(this Listing_Standard l,
                                             ref float val,
                                             ref string buffer,
                                             float min = 0f,
                                             float max = float.MaxValue,
                                             float roundTo = -1,
                                             string tooltip = null)
    {
        Rect contentRect = l.GetRect(22f);
        if (!l.BoundingRectCached.HasValue || contentRect.Overlaps(l.BoundingRectCached.Value))
        {
            //do slider
            float num = Widgets.HorizontalSlider(contentRect.LeftPart(0.75f).Rounded(), val, min, max, true, roundTo: roundTo);
            if (num != val)
                SoundDefOf.DragSlider.PlayOneShotOnCamera();

            val = num;

            //do text entry
            Widgets.TextFieldNumeric(contentRect.RightPart(0.24f).Rounded(), ref val, ref buffer, min, max);

            //do tooltip
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(contentRect))
                    Widgets.DrawHighlight(contentRect);

                TooltipHandler.TipRegion(contentRect, (TipSignal)tooltip);
            }

            l.Gap(l.verticalSpacing);
        }

        return contentRect;
    }

    internal static Rect SliderWithTextField(this Listing_Standard l, ref int val, ref string buffer, int min = 0, int max = int.MaxValue, int roundTo = -1, string tooltip = null)
    {
        float valFloat = val;
        Rect r = l.SliderWithTextField(ref valFloat, ref buffer, min, max, roundTo > 1 ? roundTo : 1, tooltip);
        val = (int)valFloat;
        return r;
    }

    internal static Rect LabeledSliderWithTextField(this Listing_Standard l,
                                                    string label,
                                                    ref float val,
                                                    ref string buffer,
                                                    float min = 0f,
                                                    float max = float.MaxValue,
                                                    float roundTo = -1,
                                                    string tooltip = null)
    {
        float height = Text.CalcHeight(label, l.ColumnWidth / 2f);
        Rect contentRect = l.GetRect(height);
        if (!l.BoundingRectCached.HasValue || contentRect.Overlaps(l.BoundingRectCached.Value))
        {
            Rect leftRect = contentRect.LeftHalf().Rounded();
            Rect midRect = contentRect.RightHalf().LeftPart(0.74f).Rounded();
            Rect rightRect = contentRect.RightHalf().RightPart(0.24f).Rounded();

            //do label
            TextAnchor textAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(leftRect, label);
            Text.Anchor = textAnchor;

            //do slider
            float num = Widgets.HorizontalSlider(midRect, val, min, max, true, roundTo: roundTo);
            if (num != val)
                SoundDefOf.DragSlider.PlayOneShotOnCamera();

            val = num;

            //do text entry
            Widgets.TextFieldNumeric(rightRect, ref val, ref buffer, min, max);

            //do tooltip
            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(contentRect))
                    Widgets.DrawHighlight(contentRect);

                TooltipHandler.TipRegion(contentRect, (TipSignal)tooltip);
            }

            l.Gap(l.verticalSpacing);
        }

        return contentRect;
    }

    internal static Rect LabeledSliderWithTextField(this Listing_Standard l,
                                                    string label,
                                                    ref int val,
                                                    ref string buffer,
                                                    int min = 0,
                                                    int max = int.MaxValue,
                                                    int roundTo = -1,
                                                    string tooltip = null)
    {
        float valFloat = val;
        Rect r = l.LabeledSliderWithTextField(label, ref valFloat, ref buffer, min, max, roundTo > 1 ? roundTo : 1, tooltip);
        val = (int)valFloat;
        return r;
    }
}

public class Listing_Tabbed : Listing_Standard
{
    private List<TabRecord> _tabs;
    private Rect _tabRect;

    public Listing_Standard BeginTabSection(List<TabRecord> tabs)
    {
        _tabs = tabs;
        //reserve remaining space for content in main listing
        _tabRect = GetRect(TabDrawer.TabHeight);
        _tabRect.yMin += TabDrawer.TabHeight; //shift down by tab size
        //start content listing. we reduced it's space at the top
        //to give tabs a position to spawn. We draw the tabs later so their
        //graphics overlay the section box and merge nicely
        Rect contentRectInner = listingRect.ContractedBy(4f);
        Listing_Standard section = BeginSection(contentRectInner.height - curY);
        return section;
    }

    public void EndTabSection(Listing_Standard tabSectionList)
    {
        EndSection(tabSectionList);
        //draw tabs now after content to blend the top
        TabDrawer.DrawTabs(_tabRect, _tabs, 500f);
    }
}