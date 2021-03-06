﻿using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class DeltaComponent : IComponent
    {
        protected InfoTimeComponent InternalComponent { get; set; }
        public DeltaSettings Settings { get; set; }
        private DeltaComponentFormatter Formatter { get; set; }

        public float PaddingTop { get { return InternalComponent.PaddingTop; } }
        public float PaddingLeft { get { return InternalComponent.PaddingLeft; } }
        public float PaddingBottom { get { return InternalComponent.PaddingBottom; } }
        public float PaddingRight { get { return InternalComponent.PaddingRight; } }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public DeltaComponent(LiveSplitState state)
        {
            Settings = new DeltaSettings()
            {
                CurrentState = state
            };
            Formatter = new DeltaComponentFormatter(Settings.Accuracy, Settings.DropDecimals);
            InternalComponent = new InfoTimeComponent(null, null, Formatter);
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        private void PrepareDraw(LiveSplitState state)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow 
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;
            Formatter.DropDecimals = Settings.DropDecimals;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.ToArgb() != Color.Transparent.ToArgb()
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.ToArgb() != Color.Transparent.ToArgb())
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public float VerticalHeight
        {
            get { return InternalComponent.VerticalHeight; }
        }

        public float MinimumWidth
        {
            get { return InternalComponent.MinimumWidth; }
        }

        public float HorizontalWidth
        {
            get { return InternalComponent.HorizontalWidth; }
        }

        public float MinimumHeight
        {
            get { return InternalComponent.MinimumHeight; }
        }

        public string ComponentName
        {
            get { return "Delta" + (Settings.Comparison == "Current Comparison" ? "" : " (" + CompositeComparisons.GetShortComparisonName(Settings.Comparison) + ")"); }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;
            var comparisonName = comparison.StartsWith("[Race] ") ? comparison.Substring(7) : comparison;

            if (InternalComponent.InformationName != comparisonName)
            {
                InternalComponent.AlternateNameText.Clear();
                InternalComponent.AlternateNameText.Add(CompositeComparisons.GetShortComparisonName(comparison));
            }
            InternalComponent.LongestString = comparisonName;
            InternalComponent.InformationName = comparisonName;

            var useLiveDelta = false;
            if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
            {
                TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, state.CurrentTimingMethod);
                var liveDelta = state.CurrentTime[state.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][state.CurrentTimingMethod];
                if (liveDelta > delta || (delta == null && liveDelta > TimeSpan.Zero))
                {
                    delta = liveDelta;
                    useLiveDelta = true;
                }
                InternalComponent.TimeValue = delta;
            }
            else if (state.CurrentPhase == TimerPhase.Ended)
            {
                InternalComponent.TimeValue = state.Run.Last().SplitTime[state.CurrentTimingMethod] - state.Run.Last().Comparisons[comparison][state.CurrentTimingMethod];
            }
            else
            {
                InternalComponent.TimeValue = null;
            }

            var color = LiveSplitStateHelper.GetSplitColor(state, InternalComponent.TimeValue, state.CurrentSplitIndex - (useLiveDelta ? 0 : 1), true, false, comparison, state.CurrentTimingMethod);
            if (color == null)
                color = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = color.Value;

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        public void Dispose()
        {
        }
    }
}
