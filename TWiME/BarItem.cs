using System;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using Extensions;

namespace TWiME {
    class BarItem {
        public int MaximumWidth, MinimumWidth;
        public bool IsBuiltIn;
        public string Path;
        public string Argument;
        public Brush ForeColour, BackColour;
        public Image Value;
        public long LastRenew;
        public TimeSpan RenewInterval;
        public string ClickExecutePath = "";
        public string PrependValue;
        public string AppendValue;
        public string[] DoNotShowMatch;
        public string[] OnlyShowOnMatch;

        public BarItem(string path, string argument="", bool builtIn = false, int minWidth = -1, int maxWidth = -1, Brush forecolour = null, Brush backcolour = null) {
            Path = path;
            Argument = argument;
            IsBuiltIn = builtIn;
            MinimumWidth = minWidth;
            MaximumWidth = maxWidth;
            BackColour = backcolour ?? new SolidBrush(
                                           Manager.Settings.ReadSettingOrDefault("Black", "General.Bar.UnselectedBackgroundColour").ToColor());
            ForeColour = forecolour ?? new SolidBrush(
                                           Manager.Settings.ReadSettingOrDefault("LightGray", "General.Bar.SelectedForeground").ToColor());
            RenewInterval = new TimeSpan(0, 0, 0, 5); //5 seconds
        }
    }
}