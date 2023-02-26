using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace SharpGraphLib
{
    public interface ILegendItem
    {
        Color Color { get; }
        bool Hidden { get; }
        bool HiddenFromLegend { get; }
        string Hint { get; }
    }

    public struct RawGraphPoint
    {
        public double X, Y;
        public int Tag;

        public RawGraphPoint(double x, double y, int tag = -1)
        {
            X = x;
            Y = y;
            Tag = tag;
        }

        public override string ToString() => (Tag == -1) ? $"(X={X}, Y={Y}, tag={Tag})" : $"(X={X}, Y={Y})";
    }

    public class Graph
    {
        List<RawGraphPoint> _Data = new List<RawGraphPoint>();
        bool _SortedDataUpdatePending = true;

        public void AddPoint(double x, double y, int tag)
        {
            _Data.Add(new RawGraphPoint(x, y, tag));
            _SortedDataUpdatePending = true;
        }

        public void AddPoint(double x, double y) => AddPoint(x, y, -1);

        public IList<RawGraphPoint> SortedPoints
        {
            get
            {
                if (_SortedDataUpdatePending)
                {
                    _Data.Sort((left, right) => left.X.CompareTo(right.X));
                    for (int i = 1; i < _Data.Count; i++)
                    {
                        if (_Data[i].X == _Data[i - 1].X)
                        {
                            _Data = RemoveDuplicateEntries(_Data);
                            break;
                        }
                    }

                    _SortedDataUpdatePending = false;
                }
                return _Data;
            }
        }

        static List<RawGraphPoint> RemoveDuplicateEntries(List<RawGraphPoint> data)
        {
            List<RawGraphPoint> result = new List<RawGraphPoint> { Capacity = data.Count };
            double? lastX = null;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].X == lastX)
                    continue;

                result.Add(data[i]);
                lastX = data[i].X;
            }
            return result;
        }

        public RawGraphPoint GetPointByIndex(int sortedPointIndex)
        {
            if (SortedPoints == null)
                throw new InvalidOperationException();
            return _Data[sortedPointIndex];
        }

        public int PointCount
        {
            get
            {
                return _Data.Count;
            }
        }

        public delegate double MathFunction(double x);
        public static Graph IterateMathFunction(MathFunction func, double start, double end, double step)
        {
            Graph gr = new Graph();
            for (double x = start; x < end; x += step)
                gr.AddPoint(x, func(x));
            return gr;
        }

        public void RemovePointsBeforeX(double cutoffX)
        {
            _Data.RemoveAll(p => p.X < cutoffX);
            _SortedDataUpdatePending = true;
        }
    }
}
