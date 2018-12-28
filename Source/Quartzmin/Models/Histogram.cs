using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quartzmin.Models
{
    public class Histogram
    {
        public List<Bar> Bars { get; set; } = new List<Bar>();

        public int ComputedWidth => Bars.Count * BarWidth;

        public int BarWidth { get; set; } = 6;

        public class Bar
        {
            public int ComputedLeft { get; internal set; }

            public double Value { get; set; }

            public double Percentage { get; set; }

            public string Tooltip { get; set; }

            public string CssClass { get; set; }
        }

        public void AddBar(double value, string tooltip, string cssClass)
        {
            Bars.Add(new Bar() { Value = value, Tooltip = tooltip, CssClass = cssClass });
        }

        internal void Layout()
        {
            double max = Bars.Max(x => x.Value);
            int i = 0;
            foreach (var b in Bars)
            {
                b.ComputedLeft = i * BarWidth;
                b.Percentage = Math.Round(b.Value / max * 100);

                i++;
            }
        }

        public static Histogram CreateEmpty()
        {
            var hst = new Histogram();

            for (int i = 0; i < 10; i++)
            {
                hst.Bars.Add(new Bar()
                {
                    Value = i % 3 + i % 5 + 1,
                    CssClass = "grey",
                });
            }
            return hst;
        }

        private static readonly Lazy<Histogram> _empty = new Lazy<Histogram>(CreateEmpty);

        public static Histogram Empty => _empty.Value;
    }
}
