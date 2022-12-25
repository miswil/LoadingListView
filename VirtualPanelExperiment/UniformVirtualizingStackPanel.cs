using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace VirtualPanelExperiment
{
    internal class UniformVirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private Size _extent = new Size(0.0, 0.0);
        private Size _viewPort = new Size(0.0, 0.0);
        private Point _offset = new Point(0.0, 0.0);

        public const int ChildHeight = 20;

        protected override Size MeasureOverride(Size constraint)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var scroll = this.ScrollOwner;
            var extent = new Size(constraint.Width, items.Count * ChildHeight);
            if (this.ItemContainerGenerator is null)
            {
                // touchint Children is necessary to generate ItemContainerGenerator
                _ = this.Children;
            }
            if (this._viewPort != constraint)
            {
                this._viewPort = constraint;
                scroll?.InvalidateScrollInfo();
            }
            if (this._extent != extent)
            {
                this._extent = extent;
                scroll?.InvalidateScrollInfo();
            }
            var visibleRange = this.CalculateVisibleItemIndex();
            this.PrepareVisibleChildren(visibleRange.first, visibleRange.last);
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                var child = this.InternalChildren[i];
                child.Measure(new Size(constraint.Width, ChildHeight));
            }
            var vacant = this._viewPort.Height + (this._offset.Y % ChildHeight) - this.InternalChildren.Count * ChildHeight;
            if (vacant > 0)
            {
                this.SetVerticalOffset(this._offset.Y - vacant);
            }
            return constraint;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var firstOffset = this._offset.Y % ChildHeight;
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                var child = this.InternalChildren[i];
                child.Arrange(new Rect(
                    new Point(-this._offset.X, i * ChildHeight - firstOffset), 
                    new Size(arrangeBounds.Width, ChildHeight)));
            }
            return arrangeBounds;
        }

        private (int first, int last) CalculateVisibleItemIndex()
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var first = (int)Math.Floor(this._offset.Y / ChildHeight);
            var visibleCount = (int)Math.Ceiling(this._viewPort.Height / ChildHeight);
            return (first, Math.Min(items.Count, first + visibleCount));
        }

        private void PrepareVisibleChildren(int firstIndex, int lastIndex)
        {
            var generator = this.ItemContainerGenerator;
            var firstPosition = generator.GeneratorPositionFromIndex(firstIndex);
            var childIndex = firstPosition.Offset == 0 ? firstPosition.Index : firstPosition.Index + 1;
            using (generator.StartAt(firstPosition, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = firstIndex; itemIndex < lastIndex; ++itemIndex, ++childIndex)
                {
                    var child = (UIElement)generator.GenerateNext(out var isNewlyRealized);
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
                        generator.PrepareItemContainer(child);
                    }
                    else
                    {
                        if (child != this.InternalChildren[childIndex])
                        {
                            this.InsertInternalChild(childIndex, child);
                        }
                    }
                }
            }
            this.CleanUpWasteChildren(firstIndex, lastIndex);
        }

        private void CleanUpWasteChildren(int firstIndex, int lastIndex)
        {
            var generator = this.ItemContainerGenerator;
            for (int i = this.InternalChildren.Count - 1; i >= 0; --i)
            {
                var childPosition = new GeneratorPosition(i, 0);
                var itemIndex = generator.IndexFromGeneratorPosition(childPosition);
                if (itemIndex < firstIndex || lastIndex < itemIndex)
                {
                    generator.Remove(childPosition, 1);
                    this.RemoveInternalChildRange(i, 1);
                }
            }
        }


        #region IScrollInfo Implementation
        public ScrollViewer? ScrollOwner { get; set; }

        public bool CanHorizontallyScroll
        { 
            get;
            set; 
        }
        public bool CanVerticallyScroll
        { 
            get; 
            set; 
        }

        public double ExtentHeight => this._extent.Height;

        public double ExtentWidth => this._extent.Width;

        public double HorizontalOffset => this._offset.X;

        public double VerticalOffset => this._offset.Y;

        public double ViewportHeight => this._viewPort.Height;

        public double ViewportWidth => this._viewPort.Width;

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || this._viewPort.Width >= this._extent.Width)
            {
                offset = 0;
            }
            else if (offset + this._viewPort.Width >= this._extent.Width)
            {
                offset = this._extent.Width - this._viewPort.Width;
            }
            this._offset.X = offset;
            this.ScrollOwner?.InvalidateScrollInfo();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || this._viewPort.Height >= this._extent.Height)
            {
                offset = 0;
            }
            else if (offset + this._viewPort.Height >= this._extent.Height)
            {
                offset = this._extent.Height - this._viewPort.Height;
            }
            if (this._offset.Y != offset)
            {
                this._offset.Y = offset;
                this.ScrollOwner?.InvalidateScrollInfo();
                this.InvalidateMeasure();
            }
        }

        public void LineDown()
        {
            this.SetVerticalOffset(this._offset.Y + ChildHeight);
        }

        public void LineUp()
        {
            this.SetVerticalOffset(this._offset.Y - ChildHeight);
        }

        public void LineLeft()
        {
            throw new System.NotImplementedException();
        }

        public void LineRight()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelDown()
        {
            this.SetVerticalOffset(this._offset.Y + ChildHeight * 3);
        }

        public void MouseWheelUp()
        {
            this.SetVerticalOffset(this._offset.Y - ChildHeight * 3);
        }

        public void MouseWheelLeft()
        {
            throw new System.NotImplementedException();
        }

        public void MouseWheelRight()
        {
            throw new System.NotImplementedException();
        }

        public void PageDown()
        {
            this.SetVerticalOffset(this._offset.Y + this._viewPort.Height);
        }

        public void PageUp()
        {
            this.SetVerticalOffset(this._offset.Y - this._viewPort.Height);
        }

        public void PageLeft()
        {
            throw new System.NotImplementedException();
        }

        public void PageRight()
        {
            throw new System.NotImplementedException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var visible = this.CalculateVisibleItemIndex();
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                if ((Visual)this.InternalChildren[i] == visual)
                {
                    var position = new GeneratorPosition(i, 0);
                    var index = this.ItemContainerGenerator.IndexFromGeneratorPosition(position);
                    if (index < visible.first || visible.last < index)
                    {
                        this.SetVerticalOffset(i * ChildHeight);
                    }
                    return rectangle;
                }
            }
            throw new ArgumentException("The visual is not a child.");
        }
        #endregion IScrollInfo Implementation
    }
}
