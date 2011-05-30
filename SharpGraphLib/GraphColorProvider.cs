using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Drawing;

namespace SharpGraphLib
{
    public partial class GraphColorProvider : Component
    {
        public GraphColorProvider()
        {
            InitializeComponent();
        }

        public GraphColorProvider(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public delegate void RandomColorGenerator(object sender, ref Color color);
        public event RandomColorGenerator GenerateRandomColor;

        static List<Color> _DefaultPredefinedColors = new List<Color> {  Color.Red, Color.Blue, Color.Lime, Color.Magenta, Color.Orange, Color.Cyan, Color.DarkOliveGreen, Color.Purple };
        
        List<Color> _PredefinedColors = new List<Color>(_DefaultPredefinedColors);
        Dictionary<Color, bool> _ColorsUsed = new Dictionary<Color, bool>();

        public bool ShouldSerializePredefinedColors()
        {
            if (_PredefinedColors.Count != _DefaultPredefinedColors.Count)
                return true;
            for (int i = 0; i < _PredefinedColors.Count; i++)
                if (_PredefinedColors[i] != _DefaultPredefinedColors[i])
                    return true;
            return false;
        }

        [Category("Colors")]
        public List<Color> PredefinedColors
        {
            get { return _PredefinedColors; }
            set { _PredefinedColors = value; }
        }

        Random _Rnd = new Random();

        public Color AllocateColor()
        {
            for (int i = 0; i < _PredefinedColors.Count; i++)
                if (!_ColorsUsed.ContainsKey(_PredefinedColors[i]))
                {
                    _ColorsUsed[_PredefinedColors[i]] = true;
                    return _PredefinedColors[i];
                }
            Color clr = Color.FromArgb((int)((uint)_Rnd.Next() | (uint)0xFF000000));
            if (GenerateRandomColor != null)
                GenerateRandomColor(this, ref clr);
            _ColorsUsed[clr] = true;
            return clr;
        }

        public void FreeColor(Color color)
        {
            _ColorsUsed.Remove(color);
        }

        public void FreeAllColors()
        {
            _ColorsUsed.Clear();
        }
    }
}
