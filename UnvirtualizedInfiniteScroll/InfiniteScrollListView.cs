using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UnvirtualizedInfiniteScroll
{
    internal class InfiniteScrollListView : DependencyObject
    {
        public static DataTemplate GetLoadTemplate(DependencyObject obj)
        {
            return (DataTemplate)obj.GetValue(LoadTemplateProperty);
        }

        public static void SetLoadTemplate(DependencyObject obj, DataTemplate value)
        {
            obj.SetValue(LoadTemplateProperty, value);
        }

        // Using a DependencyProperty as the backing store for LoadTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadTemplateProperty =
            DependencyProperty.RegisterAttached("LoadTemplate", typeof(DataTemplate), typeof(InfiniteScrollListView), new PropertyMetadata(null));

        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(InfiniteScrollListView), new PropertyMetadata(null, CommandChanged));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListView listView)
            {
                return;
            }
            switch (e.OldValue, e.NewValue)
            {
                case (ICommand, not ICommand):
                    listView.RemoveHandler(ScrollViewer.ScrollChangedEvent, (ScrollChangedEventHandler)ListViewScrolled);
                    RemoveItemsChangedEventHandler(listView);
                    break;
                case (not ICommand, ICommand command):
                    listView.AddHandler(ScrollViewer.ScrollChangedEvent, (ScrollChangedEventHandler)ListViewScrolled);
                    listView.Loaded += ListView_Loaded;
                    if (command.CanExecute(null))
                    {
                        command.Execute(null);
                    }
                    break;
                default:
                    break;
            }
        }

        private static void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListView listView)
            {
                return;
            }
            AddItemsChangedEventHandler(listView);
            listView.Loaded -= ListView_Loaded;
        }

        private static void AddItemsChangedEventHandler(ListView listView)
        {
            var itemsPresenter = listView.Template.FindName("ItemsPresenter", listView) as ItemsPresenter;
            if (itemsPresenter != null)
            {
                itemsPresenter.SizeChanged += ItemsPresenter_SizeChanged;
            }
        }

        private static void RemoveItemsChangedEventHandler(ListView listView)
        {
            var itemsPresenter = listView.Template.FindName("ItemsPresenter", listView) as ItemsPresenter;
            if (itemsPresenter != null)
            {
                itemsPresenter.SizeChanged -= ItemsPresenter_SizeChanged;
            }
        }

        private static void ItemsPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ItemsPresenter itemsPresenter &&
                itemsPresenter.TemplatedParent is ListView listView)
            {
                LoadNextChildren(listView);
            }
        }

        private static void ListViewScrolled(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0)
            {
                return;
            }
            if (sender is not ListView listView)
            {
                return;
            }
            LoadNextChildren(listView);
        }

        private static void LoadNextChildren(ListView listView)
        {
            if (GetCommand(listView) is not ICommand command)
            {
                return;
            }
            if (!command.CanExecute(null))
            {
                return;
            }
            var partLoad = listView.Template.FindName("PART_Load", listView) as Control;
            if (partLoad is null)
            {
                return;
            }
            var loadPositionRelativeToListView = partLoad.TranslatePoint(new(), listView);
            var isLoadNeeded = loadPositionRelativeToListView.Y < listView.ActualHeight;
            if (isLoadNeeded)
            {
                command.Execute(null);
            }
        }
    }
}
