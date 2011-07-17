﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Extensions {
    public static class stringExtensions {
        public static int Width(this string str, Font withFont) {
            int width = TextRenderer.MeasureText(str, withFont).Width;
            return width;
        }
        public static int Height(this string str, Font withFont) {
            int height = TextRenderer.MeasureText(str, withFont).Height;
            return height;
        }
        public static string With(this string str, params object[] formatWith) {
            return String.Format(str, formatWith);
        }
    }

}
