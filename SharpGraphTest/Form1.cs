using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SharpGraphLib;

namespace SharpGraphTest
{
    public partial class Form1 : Form
    {
        class GraphTemplate
        {
            public readonly Graph.MathFunction Func;
            string _Description;

            public override string ToString()
            {
                return _Description;
            }

            public GraphTemplate(string desc, Graph.MathFunction func)
            {
                Func = func;
                _Description = desc;
            }
        }

        InteractiveGraphViewer.Tracker m_MouseLocationTracker;
        InteractiveGraphViewer.FloatingHint m_Hint;

        public Form1()
        {
            InitializeComponent();

            comboBox1.Items.Add(new GraphTemplate("x * 10", x => x * 10));
            comboBox1.Items.Add(new GraphTemplate("x^2", x => x * x));
            comboBox1.Items.Add(new GraphTemplate("x^3", x => x * x * x));
            comboBox1.Items.Add(new GraphTemplate("x * sin(x)", x => x * Math.Sin(x)));
            comboBox1.Items.Add(new GraphTemplate("exp(x)", x => Math.Exp(x)));
            comboBox1.Items.Add(new GraphTemplate("ln(x)", x => Math.Log(x)));
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;

            m_MouseLocationTracker = graphViewer1.CreateTracker(Color.Lime);
            m_Hint = graphViewer1.CreateFloatingHint();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Get selected graph template (search for "new GraphTemplate" to see how they are created)
            GraphTemplate template = comboBox1.SelectedItem as GraphTemplate;

            //Iterate the function (compute a list of (X,Y) points)
            Graph graph = Graph.IterateMathFunction(template.Func, (double)numericUpDown1.Value, (double)numericUpDown2.Value, (double)numericUpDown3.Value);

            //Ensure that we use a unique color for the graph
            Color graphColor = graphColorProvider1.AllocateColor();

            //Set hint based on the function description
            string hint = template.ToString();

            //Add the graph (collection of (X,Y) points) to the viewer
            var displayedGraph = graphViewer1.AddGraph(graph, graphColor, 3, hint);
            if (checkBox7.Checked)
                displayedGraph.DefaultPointMarkingStyle = GraphViewer.DisplayedGraph.PointMarkingStyle.Circle;
        }

        private void graphViewer1_TransformY(object sender, bool forward, ref double Value)
        {
            //Perform selected transformation on the Y value
            switch (comboBox2.SelectedIndex)
            {
                case 0: //Linear
                    return;
                case 1: //Ln
                    if (forward)
                        Value = Math.Log(Value);
                    else
                        Value = Math.Exp(Value);
                    return;
                case 2: //Log10
                    if (forward)
                        Value = Math.Log10(Value);
                    else
                        Value = Math.Pow(10, Value);
                    return;
                default:
                    throw new InvalidOperationException("Unexpected scaling mode");
            }
        }

        SharpGraphLib.GraphViewer.DisplayedGraph.DisplayedPoint m_HighlightedPoint;

        private void graphViewer1_MouseMove(InteractiveGraphViewer sender, GraphMouseEventArgs e)
        {
            if (checkBox1.Checked)
            {
                m_MouseLocationTracker.X = e.DataX;
                m_MouseLocationTracker.Y = e.DataY;
                m_MouseLocationTracker.Hidden = false;
            }
            else
                m_MouseLocationTracker.Hidden = true;

            m_Hint.Hidden = true;
            if (m_HighlightedPoint != null)
                m_HighlightedPoint.MarkerStyle = GraphViewer.DisplayedGraph.PointMarkingStyle.Undefined;    //Default
            if (checkBox2.Checked)
            {
                int distanceSquare;
                var point = graphViewer1.FindNearestGraphPoint(e.DataX, e.DataY, false, out distanceSquare);
                if (point != null && distanceSquare <= 400)
                {
                    m_HighlightedPoint = point.NearestReferencePoint;
                    point.NearestReferencePoint.MarkerStyle = GraphViewer.DisplayedGraph.PointMarkingStyle.Square;

                    if (checkBox3.Checked && (point != null))
                    {
                        m_Hint.FillColor = Color.FromArgb(200, Color.LightGreen);
                        m_Hint.Show(e, string.Format("{2}\nNearest X = {0:f2}\nNearest Y = {1:f2}", m_HighlightedPoint.X, m_HighlightedPoint.Y, point.Graph.Hint));
                    }

                    graphViewer1.ActiveGraph = point.Graph;
                }
                else
                {
                    if (checkBox3.Checked && (point != null))
                    {
                        m_Hint.FillColor = Color.FromArgb(200, Color.LightYellow);
                        m_Hint.Show(e, string.Format("{2}\nX = {0:f2}\nY (interpolated)={1:f2}", point.X, point.Y, point.Graph.Hint));
                    }
                    graphViewer1.ActiveGraph = null;
                }
            }
            if (graphViewer1.PreviewRectangle.Visible)
            {
                graphViewer1.PreviewRectangle.X2 = e.DataX;
                graphViewer1.PreviewRectangle.Y2 = e.DataY;
            }
        }

        private void graphViewer1_MouseDown(InteractiveGraphViewer sender, GraphMouseEventArgs e)
        {
            if (((Control.ModifierKeys & Keys.Control) != Keys.None) && checkBox4.Checked)
            {
                graphViewer1.PreviewRectangle.X1 = e.DataX;
                graphViewer1.PreviewRectangle.Y1 = e.DataY;
                graphViewer1.PreviewRectangle.X2 = e.DataX;
                graphViewer1.PreviewRectangle.Y2 = e.DataY;
                graphViewer1.PreviewRectangle.Visible = true;
            }
            if ((e.Button == MouseButtons.Middle) && checkBox6.Checked)
            {
                graphViewer1.CreateOrCloseFullscreenClone();
            }
        }

        private void graphViewer1_MouseUp(InteractiveGraphViewer sender, GraphMouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left && graphViewer1.PreviewRectangle.Visible))
                graphViewer1.ForceNewBounds(graphViewer1.PreviewRectangle.MinX, graphViewer1.PreviewRectangle.MaxX, graphViewer1.PreviewRectangle.MinY, graphViewer1.PreviewRectangle.MaxY);
            else
                graphViewer1.ForceCustomBounds = false;
            graphViewer1.PreviewRectangle.Visible = false;
        }

        private void removeGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var gr = legendControl1.ActiveItem as GraphViewer.DisplayedGraph;
            if (gr != null)
            {
                graphColorProvider1.FreeColor(gr.Color);
                graphViewer1.RemoveGraph(gr);
            }
        }

        private void lineWidthToolStripMenuItem_TextChanged(object sender, EventArgs e)
        {
            var gr = legendControl1.ActiveItem as GraphViewer.DisplayedGraph;
            int val;
            int.TryParse((sender as ToolStripComboBox).Text, out val);
            if ((gr != null) && (val != 0))
                gr.LineWidth = val;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            graphViewer1.ResetGraphs();
            graphColorProvider1.FreeAllColors();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            graphViewer1.UpdateScaling();
            graphViewer1.Invalidate();
        }

        private void graphViewer1_MouseLeave(object sender, EventArgs e)
        {
            m_MouseLocationTracker.Hidden = true;
            if (m_HighlightedPoint != null)
                m_HighlightedPoint.MarkerStyle = GraphViewer.DisplayedGraph.PointMarkingStyle.Undefined;    //Default
            m_Hint.Hidden = true;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            graphViewer1.EmbeddedLegend = checkBox5.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            graphViewer1.IndividualScaling = checkBox8.Checked;
        }
    }
}
