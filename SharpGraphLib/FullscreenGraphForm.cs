using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SharpGraphLib
{
    public partial class FullscreenGraphForm : Form
    {
        Control _OriginalParent;
        Point _OriginalLocation;
        Size _OriginalSize;
        DockStyle _OriginalDock;
        AnchorStyles _OriginalAnchors;

        InteractiveGraphViewer _Viewer;

        public FullscreenGraphForm(InteractiveGraphViewer viewer)
        {
            InitializeComponent();
            _OriginalParent = viewer.Parent;
            _OriginalLocation = viewer.Location;
            _OriginalSize = viewer.Size;
            _OriginalDock = viewer.Dock;
            _OriginalAnchors = viewer.Anchor;

            Text = viewer.MaximizedModeTitle;

            viewer.Parent = this;
            viewer.Dock = DockStyle.Fill;
            viewer._Maximized = true;
            _Viewer = viewer;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _Viewer.Parent = _OriginalParent;
            _Viewer.Location = _OriginalLocation;
            _Viewer.Size = _OriginalSize;
            _Viewer.Dock = _OriginalDock;
            _Viewer.Anchor = _OriginalAnchors;
            _Viewer._Maximized = false;
        }
    }
}
