using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace UniformVirtualizingInfiniteScroll
{
    internal class UniformInfiniteScrollableVirtualizingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private Size _extent = new Size(0.0, 0.0);
        private Size _viewPort = new Size(0.0, 0.0);
        private Point _offset = new Point(0.0, 0.0);
        private ContentControl? _contentControl;

        public double ContainerHeight
        {
            get { return (double)GetValue(ContainerHeightProperty); }
            set { SetValue(ContainerHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContainerHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContainerHeightProperty =
            DependencyProperty.Register("ContainerHeight", typeof(double), typeof(UniformInfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(0.0));

        public DataTemplate LoadingTemplate
        {
            get { return (DataTemplate)GetValue(LoadingTemplateProperty); }
            set { SetValue(LoadingTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadingMark.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadingTemplateProperty =
            DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(UniformInfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null, LoadingTemplateChanged));

        private static void LoadingTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var contentControl = ((UniformInfiniteScrollableVirtualizingStackPanel)d)._contentControl;
            if (contentControl is not null)
            {
                contentControl.ContentTemplate = (DataTemplate)e.NewValue;
            }
        }

        public ICommand? Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(UniformInfiniteScrollableVirtualizingStackPanel), new PropertyMetadata(null));

        protected override Size MeasureOverride(Size constraint)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var isGrouping = itemsControl.IsGrouping;
            var scroll = this.ScrollOwner;
            if (this.ItemContainerGenerator is null)
            {
                // Touching Children is necessary to generate ItemContainerGenerator
                _ = this.Children;
            }
            if (isGrouping)
            {
                this.PrepareVisibleChildren(0, items.Count + 1);
                if (scroll != null &&
                    (this._viewPort.Height != scroll.ViewportHeight ||
                    this._viewPort.Width != scroll.ViewportWidth))
                {
                    this._viewPort = new Size(scroll.ViewportWidth, scroll.ViewportHeight);
                    scroll?.InvalidateScrollInfo();
                }
            }
            else
            {
                if (this._viewPort != constraint)
                {
                    this._viewPort = constraint;
                    scroll?.InvalidateScrollInfo();
                }
                var visibleRange = this.CalculateVisibleItemIndex();
                this.PrepareVisibleChildren(visibleRange.first, visibleRange.last);
            }
            var maxWidth = 0.0;
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                var child = this.InternalChildren[i];
                child.Measure(new Size(double.PositiveInfinity, isGrouping ? constraint.Height : this.ContainerHeight));
                maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            }
            var extentHeight = (items.Count + 1) * this.ContainerHeight;
            var extent = new Size(maxWidth, extentHeight);
            if (this._extent != extent)
            {
                this._extent = extent;
                scroll?.InvalidateScrollInfo();
            }
            var vacant = this._viewPort.Height + (this._offset.Y % this.ContainerHeight) - this.InternalChildren.Count * this.ContainerHeight;
            if (vacant > 0)
            {
                this.SetVerticalOffset(this._offset.Y - vacant);
            }
            return new Size(maxWidth, isGrouping ? extentHeight : constraint.Height);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var isGrouping = itemsControl.IsGrouping;
            var firstOffset = isGrouping ? 0.0 : this._offset.Y % this.ContainerHeight;
            var childHeightSum = 0.0;
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                var child = this.InternalChildren[i];
                child.Arrange(new Rect(
                    new Point(-this._offset.X, childHeightSum - firstOffset),
                    new Size(arrangeBounds.Width, isGrouping ? child.DesiredSize.Height : this.ContainerHeight)));
                childHeightSum += child.RenderSize.Height;
            }

            if (isGrouping ?
                childHeightSum - this.ContainerHeight - ViewportHeight <= this.VerticalOffset:
                this._contentControl != null)
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    if (this.Command?.CanExecute(null) ?? false)
                    {
                        this.Command.Execute(null);
                    }
                });
            }
            return arrangeBounds;
        }

        private (int first, int last) CalculateVisibleItemIndex()
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var items = itemsControl.Items;
            var first = (int)Math.Floor(this._offset.Y / this.ContainerHeight);
            var visibleCount = (int)Math.Ceiling(this._viewPort.Height / this.ContainerHeight);
            return (first, Math.Min(items.Count + 1, first + visibleCount));
        }

        private void PrepareVisibleChildren(int firstIndex, int lastIndex)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var isGrouping = itemsControl.IsGrouping;
            var items = itemsControl.Items;
            var generator = this.ItemContainerGenerator;
            var firstPosition = generator.GeneratorPositionFromIndex(firstIndex);
            var childIndex = firstPosition.Offset == 0 ? firstPosition.Index : firstPosition.Index + 1;
            using (generator.StartAt(firstPosition, GeneratorDirection.Forward, true))
            {
                for (int itemIndex = firstIndex; itemIndex < lastIndex; ++itemIndex, ++childIndex)
                {
                    bool isNewlyRealized;
                    UIElement child;
                    if (itemIndex == items.Count)
                    {
                        isNewlyRealized = this._contentControl is null;
                        child = this._contentControl ??= new ContentControl
                        {
                            ContentTemplate = this.LoadingTemplate,
                            Height = this.ContainerHeight,
                        };
                    }
                    else
                    {
                        child = (UIElement)generator.GenerateNext(out isNewlyRealized);
                        if (isNewlyRealized)
                        {
                            generator.PrepareItemContainer(child);
                        }
                    }
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
                    }
                    else
                    {
                        if ((!isGrouping || (child != null && childIndex < this.InternalChildren.Count))
                            &&
                            child != this.InternalChildren[childIndex])
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
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var isGrouping = itemsControl.IsGrouping;
            var items = itemsControl.Items;
            var loadingUnnecessary = items.Count >= lastIndex;
            if (loadingUnnecessary && this._contentControl is not null)
            {
                this._contentControl = null;
                this.RemoveInternalChildRange(this.InternalChildren.Count - 1, 1);
                lastIndex--;
            }
            var generatedItemsCount = loadingUnnecessary ? this.InternalChildren.Count - 1 : this.InternalChildren.Count - 2;
            var generator = this.ItemContainerGenerator;
            for (int i = generatedItemsCount; i >= 0; --i)
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

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (this._contentControl != null)
                    {
                        AddInternalChild(this._contentControl);
                    }
                    break;
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
            if (this._offset.X != offset)
            {
                this._offset.X = offset;
                this.ScrollOwner?.InvalidateScrollInfo();
                this.InvalidateMeasure();
            }
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
            this.SetVerticalOffset(this._offset.Y + this.ContainerHeight);
        }

        public void LineUp()
        {
            this.SetVerticalOffset(this._offset.Y - this.ContainerHeight);
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
            this.SetVerticalOffset(this._offset.Y + this.ContainerHeight * 3);
        }

        public void MouseWheelUp()
        {
            this.SetVerticalOffset(this._offset.Y - this.ContainerHeight * 3);
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
