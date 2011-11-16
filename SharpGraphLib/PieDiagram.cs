using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SharpGraphLib
{
    public partial class PieDiagram : UserControl
    {
        public PieDiagram()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        private class Value
        {
            public string Hint;
            public Color FillColor;
            public double Val;
        }

        List<Value> Values = new List<Value>();
        readonly bool _ShowPercentages = true;

        public int AddValue(string hint, Color color)
        {
            Values.Add(new Value { Hint = hint, FillColor = color, Val = 0 });
            Invalidate();
            return Values.Count - 1;
        }

        public void SetValue(int index, double val)
        {
            Values[index].Val = val;
            Invalidate();
        }

        public enum RelativePosition
        {
            Top,
            Left,
            Right,
            Bottom,
        }

        RelativePosition _LegendAlignment;

        public RelativePosition LegendAlignment
        {
            get { return _LegendAlignment; }
            set { _LegendAlignment = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            base.OnPaint(e);

            double sum = 0;
            foreach (Value val in Values) sum += val.Val;
            if (sum == 0)
                sum = 1;

            string[] labels = new string[Values.Count];

            int maxLabelWidth = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                if (_ShowPercentages)
                    labels[i] = Values[i].Hint + string.Format(" ({0:f1}%)", (100.0 * Values[i].Val) / sum);
                else
                    labels[i] = Values[i].Hint;

                SizeF size = e.Graphics.MeasureString(labels[i], Font);
                maxLabelWidth = Math.Max(maxLabelWidth, (int)size.Width + 1);
            }

            const int Margin = 5;
            int legendWidth = (int)(maxLabelWidth + Font.Height * 1.5 + Margin * 2);
            int legendHeight = (int)(Font.Height * Values.Count * 1.5);

            int dimension;
            switch(_LegendAlignment)
            {
                case RelativePosition.Top:
                case RelativePosition.Bottom:
                    dimension = Math.Min(Width - Margin * 2, Height - legendHeight - Margin * 2);
                    break;
                default:
                    dimension = Math.Min(Width - legendWidth - Margin * 2, Height - Margin * 2);
                    break;
            }
            

            Color[] colors = new Color[Values.Count];
            double[] vals = new double[Values.Count];

            for (int i = 0; i < Values.Count; i++)
            {
                colors[i] = Values[i].FillColor;
                vals[i] = Values[i].Val;
            }

            if (dimension <= 0)
                return;

            switch (_LegendAlignment)
            {
                case RelativePosition.Top:
                    DrawPieDiagram(e.Graphics, new Rectangle((Width - dimension) / 2, (Height + legendHeight - dimension) / 2, dimension, dimension), colors, vals);
                    break;
                case RelativePosition.Left:
                    DrawPieDiagram(e.Graphics, new Rectangle((Width - legendWidth - dimension) / 2 + legendWidth, (Height - dimension) / 2, dimension, dimension), colors, vals);
                    break;
                case RelativePosition.Right:
                    DrawPieDiagram(e.Graphics, new Rectangle((Width - legendWidth - dimension) / 2, (Height - dimension) / 2, dimension, dimension), colors, vals);
                    break;
                case RelativePosition.Bottom:
                    DrawPieDiagram(e.Graphics, new Rectangle((Width - dimension) / 2, (Height - legendHeight - dimension) / 2, dimension, dimension), colors, vals);
                    break;
            }

            int x, y;
            switch (_LegendAlignment)
            {
                case RelativePosition.Top:
                    x = (Width - legendWidth) / 2;
                    y = Margin;
                    break;
                case RelativePosition.Left:
                    x = Margin;
                    y = (Height - legendHeight) / 2;
                    break;
                case RelativePosition.Right:
                    x = Width - (int)(maxLabelWidth + Font.Height * 1.5 - Margin);
                    y = (Height - legendHeight) / 2;
                    break;
                case RelativePosition.Bottom:
                    x = (Width - legendWidth) / 2;
                    y = Height - legendHeight;
                    break;
                default:
                    return;
            }

            Pen pen = Pens.Black;
            for (int i = 0; i < Values.Count; i++ )
            {
                e.Graphics.FillRectangle(new SolidBrush(Values[i].FillColor), x, y, Font.Height, Font.Height);
                e.Graphics.DrawRectangle(pen, x, y, Font.Height, Font.Height);
                e.Graphics.DrawString(labels[i], Font, Brushes.Black, x + Font.Height * 1.5F, y);

                y += (int)(Font.Height * 1.5);
            }

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        public static void DrawPieDiagram(Graphics gr, Rectangle rect, Color[] colors, double[] values)
        {
            UInt64 sum = 0;
            foreach (UInt64 val in values) sum += val;
            if (sum == 0)
                sum = 1;

            Pen pen = new Pen(Color.Black, 2);

            float angle = 0;
            for (int i = 0; i < values.Length; i++)
            {
                float portion = (((float)values[i]) * 360) / sum;
                
                float newAngle = angle + portion;

                gr.FillPie(new SolidBrush(colors[i % colors.Length]), rect, angle, portion);
                //gr.DrawPie(pen, rect, angle, portion);
                angle = newAngle;
            }

            gr.DrawEllipse(pen, rect);

        }
    }
}
