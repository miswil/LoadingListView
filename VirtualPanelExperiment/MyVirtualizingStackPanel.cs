using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VirtualPanelExperiment
{
    internal class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        protected override Size MeasureOverride(Size constraint)
        {
            var ret = base.MeasureOverride(constraint);
            return ret;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var ret = base.ArrangeOverride(arrangeSize);
            return ret;
        }
    }
}
