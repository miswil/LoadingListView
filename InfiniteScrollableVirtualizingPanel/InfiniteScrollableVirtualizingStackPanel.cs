using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace InfiniteScrollableVirtualizingPanel
{
    internal class InfiniteScrollableVirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private Size _extent = new Size(0.0, 0.0);
        private Size _viewPort = new Size(0.0, 0.0);
        private Point _offset = new Point(0.0, 0.0);
        private double _averageChildHeight = 0.0;
        private ContentControl _contentControl = new();
        private bool _isLoading;

        public DataTemplate? LoadingTemplate
        {
            get { return (DataTemplate)GetValue(LoadingTemplateProperty); }
            set { SetValue(LoadingTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadingMark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingTemplateProperty =
            DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(InfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null, LoadingTemplateChanged));

        private static void LoadingTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InfiniteScrollableVirtualizingStackPanel)d)._contentControl.ContentTemplate = (DataTemplate?)e.NewValue;
        }

        public ICommand? Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(InfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null));

        protected override Size MeasureOverride(Size constraint)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var scroll = this.ScrollOwner;
            if (this.ItemContainerGenerator is null)
            {
                // Touching Children is necessary to generate ItemContainerGenerator
                _ = this.Children;
            }
            if (this._viewPort != constraint)
            {
                this._viewPort = constraint;
                scroll?.InvalidateScrollInfo();
            }
            var visibleFirst = this.CalculateVisibleItemIndexStart();
            var generator = this.ItemContainerGenerator;
            var firstPosition = generator!.GeneratorPositionFromIndex(visibleFirst);
            var childIndex = firstPosition.Offset == 0 ? firstPosition.Index : firstPosition.Index + 1;
            var childMaxWidth = 0.0;
            var childHeightSum = 0.0;
            var visibleLast = visibleFirst;
            using (generator.StartAt(firstPosition, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = visibleFirst; itemIndex < items.Count; ++itemIndex, ++childIndex)
                {
                    visibleLast = itemIndex;
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
                    child.Measure(constraint);
                    childMaxWidth = Math.Max(childMaxWidth, child.DesiredSize.Width);
                    childHeightSum += child.DesiredSize.Height;
                    if (childHeightSum > this._viewPort.Height)
                    {
                        break;
                    }
                }
            }

            if (childHeightSum < this._viewPort.Height)
            {
                if (!this._isLoading)
                {
                    this._isLoading = true;
                    this._contentControl.Measure(constraint);
                    this.AddInternalChild(this._contentControl);
                }
            }
            else
            {
                this._isLoading = false;
                this.RemoveInternalChildRange(this.InternalChildren.Count - 1, 1);
            }

            this.CleanUpWasteChildren(visibleFirst, visibleLast);
            this._averageChildHeight = childHeightSum / (visibleLast - visibleFirst + 1);

            var extent = new Size(
                childMaxWidth,
                this._averageChildHeight * items.Count + this._contentControl.DesiredSize.Height);
            if (this._extent != extent)
            {
                this._extent = extent;
                scroll?.InvalidateScrollInfo();
            }
            var vacant = this._viewPort.Height + this._offset.Y % this._averageChildHeight - childHeightSum - (this._isLoading ? this._contentControl.DesiredSize.Height : 0.0);
            if (vacant > 0.0)
            {
                this.SetVerticalOffset(this._offset.Y - vacant);
            }
            return new Size(childMaxWidth, childHeightSum);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var firstOffset = this._offset.Y % this._averageChildHeight;
            var childHeightSum = 0.0;
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                var child = this.InternalChildren[i];
                child.Arrange(new Rect(
                    new Point(-this._offset.X, childHeightSum - firstOffset),
                    new Size(arrangeBounds.Width, child.DesiredSize.Height)));
                childHeightSum += child.RenderSize.Height;
            }
            this.Dispatcher.BeginInvoke(() =>
            {
                if (this.Command?.CanExecute(null) ?? false)
                {
                    this.Command.Execute(null);
                }
            });
            return arrangeBounds;
        }

        private int CalculateVisibleItemIndexStart()
        {
            if (_averageChildHeight == 0.0)
            {
                return 0;
            }
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var first = (int)Math.Floor(this._offset.Y / this._averageChildHeight);
            return first;
        }

        private void CleanUpWasteChildren(int firstIndex, int lastIndex)
        {
            var generator = this.ItemContainerGenerator;
            for (int i = this.InternalChildren.Count - 1 - (this._isLoading ? 1 : 0); i >= 0; --i)
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
            this.SetVerticalOffset(this._offset.Y + this._averageChildHeight);
        }

        public void LineUp()
        {
            this.SetVerticalOffset(this._offset.Y - this._averageChildHeight);
        }

        public void LineLeft()
        {
            throw new NotImplementedException();
        }

        public void LineRight()
        {
            throw new NotImplementedException();
        }

        public void MouseWheelDown()
        {
            this.SetVerticalOffset(this._offset.Y + this._averageChildHeight * 3);
        }

        public void MouseWheelUp()
        {
            this.SetVerticalOffset(this._offset.Y - this._averageChildHeight * 3);
        }

        public void MouseWheelLeft()
        {
            throw new NotImplementedException();
        }

        public void MouseWheelRight()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void PageRight()
        {
            throw new NotImplementedException();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                if (this.InternalChildren[i] == visual)
                {
                    return rectangle;
                }
            }
            throw new ArgumentException("The visual is not a child.");
        }
        #endregion IScrollInfo Implementation
    }
}
