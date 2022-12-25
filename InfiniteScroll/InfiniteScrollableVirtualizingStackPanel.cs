using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace InfiniteScroll
{
    public class InfiniteScrollableVirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private Size _extentSize;
        private Size _viewPortSize;
        private Point _offset;
        private double _averageChildHeight = double.NaN;

        public DataTemplate LoadingTemplate
        {
            get { return (DataTemplate)GetValue(LoadingMarkProperty); }
            set { SetValue(LoadingMarkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadingMark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingMarkProperty =
            DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(InfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(InfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null));

        #region Virtualizing
        protected override Size MeasureOverride(Size availableSize)
        {
            if (this._viewPortSize != availableSize)
            {
                this.ScrollOwner?.InvalidateScrollInfo();
                this._viewPortSize = availableSize;
            }
            if (this.ItemContainerGenerator is null)
            {
                return new Size(0, 0);
            }
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var visibleRange = this.CalculateVisibleRange();
            var firstPosition = this.ItemContainerGenerator.GeneratorPositionFromIndex(visibleRange.first);
            var childIndex = firstPosition.Offset == 0 ? firstPosition.Index : firstPosition.Index + 1;
            var childrenHeight = 0.0;
            using (this.ItemContainerGenerator.StartAt(firstPosition, GeneratorDirection.Forward))
            {
                for (int i = visibleRange.first; i < visibleRange.last; ++i)
                {
                    var child = (UIElement)this.ItemContainerGenerator.GenerateNext(out var isNewlyRealized);
                    if (isNewlyRealized)
                    {
                        if (childIndex < this.InternalChildren.Count)
                        {
                            this.InsertInternalChild(childIndex, child);
                        }
                        else
                        {
                            this.AddInternalChild(child);
                        }
                        this.ItemContainerGenerator.PrepareItemContainer(child);
                    }
                    child.Measure(availableSize);
                    childrenHeight += child.DesiredSize.Height;
                }
            }
            this.CleanUpWasteChildren(visibleRange);
            this.UpdateAverageChildHeight();
            if (itemsControl.Items.Count != this.InternalChildren.Count
                && childrenHeight < this.ViewportHeight)
            {
                this.InvalidateMeasure();
            }
            var newExtent = new Size(availableSize.Width, this._averageChildHeight * itemsControl.Items.Count);
            if (this._extentSize != newExtent)
            {
                this._extentSize = newExtent;
                this.ScrollOwner?.InvalidateScrollInfo();
            }
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var height = 0.0;
            foreach (UIElement child in this.InternalChildren)
            {
                child.Arrange(new Rect(new Point(0, height), finalSize));
                height += child.RenderSize.Height;
            }
            return finalSize;
        }

        private (int first, int last) CalculateVisibleRange()
        {
            if (double.IsNaN(this._averageChildHeight))
            {
                // initial calculate
                return (0, 1);
            }
            else
            {
                var itemsControl = ItemsControl.GetItemsOwner(this);
                var first = (int)Math.Floor(this._offset.Y / this._averageChildHeight);
                var last = Math.Min(
                    itemsControl.Items.Count,
                    first + (int)Math.Floor(this._viewPortSize.Height / this._averageChildHeight) + 1);
                return (first, last);
            }
        }

        private void CleanUpWasteChildren((int first, int last) visibleRange)
        {
            for (int i = this.InternalChildren.Count; i >= 0; --i)
            {
                var childPosition = new GeneratorPosition(i, 0);
                var itemIndex = this.ItemContainerGenerator.IndexFromGeneratorPosition(childPosition);
                if (itemIndex > visibleRange.first || visibleRange.last < itemIndex)
                {
                    this.ItemContainerGenerator.Remove(childPosition, 1);
                    this.RemoveInternalChildRange(i, 1);
                }
            }
        }

        private void UpdateAverageChildHeight()
        {
            this._averageChildHeight = 
                this.InternalChildren.Cast<UIElement>().Average(ui => ui.DesiredSize.Height);
        }
        #endregion Virtualizing

        #region IScrollInfo implementation
        public bool CanHorizontallyScroll { get; set; }


        public bool CanVerticallyScroll { get; set; }

        public double ExtentHeight => this._extentSize.Height;

        public double ExtentWidth => this._extentSize.Width;

        public double HorizontalOffset => this._offset.Y;

        public double VerticalOffset => this._offset.X;

        public ScrollViewer? ScrollOwner { get; set; }

        public double ViewportHeight => this._viewPortSize.Height;

        public double ViewportWidth => this._viewPortSize.Width;

        public void LineDown()
        {
            if (this.VerticalOffset + this.ViewportHeight < this.ViewportHeight)
            {
                this.LineDown();
            }
            else
            {

            }
        }

        public void LineLeft()
        {
            throw new System.NotImplementedException();
        }

        public void LineRight()
        {
            throw new System.NotImplementedException();
        }

        public void LineUp()
        {
            throw new System.NotImplementedException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelDown()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelLeft()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelRight()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelUp()
        {
            throw new System.NotImplementedException();
        }

        public void PageDown()
        {
            throw new System.NotImplementedException();
        }

        public void PageLeft()
        {
            throw new System.NotImplementedException();
        }

        public void PageRight()
        {
            throw new System.NotImplementedException();
        }

        public void PageUp()
        {
            throw new System.NotImplementedException();
        }

        public void SetHorizontalOffset(double offset)
        {
            throw new System.NotImplementedException();
        }

        public void SetVerticalOffset(double offset)
        {
            throw new System.NotImplementedException();
        }
        #endregion IScrollInfo implementation
    }
}
