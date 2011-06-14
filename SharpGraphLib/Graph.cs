using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace SharpGraphLib
{
    public interface ILegendItem
    {
        Color Color { get; }
        bool Hidden { get; }
        string Hint { get; }
    }

    public class Graph
    {
        Dictionary<double, double> _Data = new Dictionary<double, double>();

        List<KeyValuePair<double, double>> _SortedData = new List<KeyValuePair<double, double>>();
        bool _SortedDataUpdatePending = true;

        public void AddPoint(double x, double y)
        {
            _Data[x] = y;
            _SortedDataUpdatePending = true;
        }

        public List<KeyValuePair<double, double>> SortedPoints
        {
            get
            {
                if (_SortedDataUpdatePending)
                {
                    _SortedData.Clear();
                    _SortedData.AddRange(_Data);
                    _SortedData.Sort((left, right) => left.Key.CompareTo(right.Key));
                    _SortedDataUpdatePending = false;
                }
                return _SortedData;
            }
        }

        public KeyValuePair<double, double> GetPointByIndex(int sortedPointIndex)
        {
            if (SortedPoints == null)
                throw new InvalidOperationException();
            return _SortedData[sortedPointIndex];
        }

        public double GetValue(double key)
        {
            return _Data[key];
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
    }
}
