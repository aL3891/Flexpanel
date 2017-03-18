using System;
using System.Collections.Generic;
using System.Linq;


#if WINDOWS_UWP
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

#if WPF
using System.Windows;
using System.Windows.Controls;
#endif

#if XAMARIN

#endif



namespace FlexPanelLayout
{
    public class Flexpanel : Panel
    {
        public static readonly DependencyProperty OrderProperty = DependencyProperty.RegisterAttached("Order", typeof(int), typeof(Flexpanel), new PropertyMetadata(int.MinValue, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty GrowProperty = DependencyProperty.RegisterAttached("Grow", typeof(int), typeof(Flexpanel), new PropertyMetadata(1, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty ShrinkProperty = DependencyProperty.RegisterAttached("Shrink", typeof(int), typeof(Flexpanel), new PropertyMetadata(1, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty AlignSelfProperty = DependencyProperty.RegisterAttached("AlignSelf", typeof(FlexAlignItems), typeof(Flexpanel), new PropertyMetadata(FlexAlignItems.NotSet, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty BasisProperty = DependencyProperty.RegisterAttached("Basis", typeof(double), typeof(Flexpanel), new PropertyMetadata(1d));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register("Direction", typeof(FlexDirection), typeof(Flexpanel), new PropertyMetadata(FlexDirection.Row, new PropertyChangedCallback((d, e) => { ((Flexpanel)d).UpdateDirection((FlexDirection)e.NewValue); ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty WrapProperty = DependencyProperty.Register("Wrap", typeof(FlexWrap), typeof(Flexpanel), new PropertyMetadata(FlexWrap.NoWrap, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty JustifyContentProperty = DependencyProperty.Register("JustifyContent", typeof(FlexJustifyContent), typeof(Flexpanel), new PropertyMetadata(FlexJustifyContent.Center, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty AlignItemsProperty = DependencyProperty.Register("AlignItems", typeof(FlexAlignItems), typeof(Flexpanel), new PropertyMetadata(FlexAlignItems.Start, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));
        public static readonly DependencyProperty AlignContentProperty = DependencyProperty.Register("AlignContent", typeof(FlexAlignContent), typeof(Flexpanel), new PropertyMetadata(FlexAlignContent.Center, new PropertyChangedCallback((d, e) => { ((UIElement)d).InvalidateMeasure(); })));

        Func<Point, double> GetPrimaryPosition, GetSecondaryPosition;
        Func<Size, double> GetPrimaryAxsis, GetSecondaryAxsis;

        Func<double, double, Size> NewSize;
        Func<double, double, Point> NewPoint;

        double startMargin, midMargin;
        Size totalContentSize;

        DependencyProperty primaryaxsisMin, primaryaxsismax, primaryaxsis;
        private List<ExtendedChild> es;
        private bool IsRow;
        private bool isReverse;
        private double totalBasis;

        public Flexpanel()
        {
            UpdateDirection(FlexDirection.Row);
        }

        private void UpdateDirection(FlexDirection dir)
        {
            switch (dir)
            {
                case FlexDirection.Row:
                case FlexDirection.RowReverse:
                    NewSize = (p, s) => new Size(p, s);
                    NewPoint = (p, s) => new Point(p, s);
                    GetPrimaryPosition = p => p.X;
                    GetSecondaryPosition = p => p.Y;
                    GetPrimaryAxsis = p => p.Width;
                    GetSecondaryAxsis = p => p.Height;
                    primaryaxsis = WidthProperty;
                    primaryaxsismax = MaxWidthProperty;
                    primaryaxsisMin = MinWidthProperty;
                    IsRow = true;
                    break;
                case FlexDirection.Column:
                case FlexDirection.ColumnReverse:
                    NewSize = (p, s) => new Size(s, p);
                    NewPoint = (p, s) => new Point(s, p);
                    GetPrimaryPosition = p => p.Y;
                    GetSecondaryPosition = p => p.X;
                    GetPrimaryAxsis = p => p.Height;
                    GetSecondaryAxsis = p => p.Width;
                    primaryaxsis = HeightProperty;
                    primaryaxsismax = MaxHeightProperty;
                    primaryaxsisMin = MinHeightProperty;
                    IsRow = false;
                    break;
                default:
                    break;
            }

            if (Direction == FlexDirection.ColumnReverse || Direction == FlexDirection.RowReverse)
                isReverse = true;
            else
                isReverse = false;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var c in Children.Cast<UIElement>())
            {
                SetChildAlignment(c);
                c.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            if (Children.Count > 0)
            {
                totalContentSize = NewSize(Children.Cast<UIElement>().Sum(c => GetPrimaryAxsis(c.DesiredSize)), Children.Cast<UIElement>().Max(c => GetSecondaryAxsis(c.DesiredSize)));
                totalBasis = Children.Cast<UIElement>().Sum(c => GetBasis(c));
                es = Children.Cast<UIElement>().Select(c => new ExtendedChild(c, GetPrimaryAxsis(c.DesiredSize), totalBasis, primaryaxsis, primaryaxsismax, primaryaxsisMin)).ToList();
            }
            else
                totalContentSize = new Size();
            
            return NewSize(
                GetPrimaryAxsis(availableSize) == double.PositiveInfinity ? GetPrimaryAxsis(totalContentSize) : GetPrimaryAxsis(availableSize),
                GetSecondaryAxsis(availableSize) == double.PositiveInfinity ? GetSecondaryAxsis(totalContentSize) : GetSecondaryAxsis(availableSize));
        }

        private void CalculateMargins(Size availableSize)
        {
            var availableSpace = GetPrimaryAxsis(availableSize);
            
            while (availableSpace > 0)
            {
                var canAcceptSpace = es.Where(cc => cc.CanUseMoreSpace()).ToList();

                if (canAcceptSpace.Count == 0)
                    break;
                if (canAcceptSpace.Count == 1 || availableSpace < 1)
                    availableSpace -= canAcceptSpace[0].AddSpace(availableSpace);
                else
                    foreach (var e in canAcceptSpace)
                        availableSpace -= e.AddSpace(availableSpace * e.basis);
            }
            
            startMargin = 0;
            midMargin = 0;

            if ((JustifyContent == FlexJustifyContent.End && !isReverse) || (JustifyContent == FlexJustifyContent.Start && isReverse))
                startMargin = availableSpace;
            else if (JustifyContent == FlexJustifyContent.Center)
                startMargin = availableSpace / 2;
            else if (JustifyContent == FlexJustifyContent.SpaceBetween)
            {
                var margin = availableSpace / (Children.Count + 1);
                midMargin = margin + ((margin + margin) / (Children.Count - 1));
            }
            else if (JustifyContent == FlexJustifyContent.SpaceAround)
                startMargin = midMargin = availableSpace / (Children.Count + 1);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {

            CalculateMargins(finalSize);

            if (Children.Count == 0)
                return finalSize;

            var anchor = NewPoint(startMargin, 0);
            var cc = isReverse ? Enumerable.Reverse(es) : es;

            foreach (var c in cc)
            {
                c.c.Arrange(new Rect(anchor, NewSize(c.current, GetSecondaryAxsis(finalSize))));
                anchor = NewPoint(GetPrimaryPosition(anchor) + c.current + midMargin, 0);
            }

            return finalSize;
        }

        private void SetChildAlignment(UIElement c)
        {
            var a = GetAlignSelf(c);
            if (a == FlexAlignItems.NotSet)
                a = AlignItems;

            switch (a)
            {
                case FlexAlignItems.NotSet:
                case FlexAlignItems.Start:
                default:
                    if (IsRow) c.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left); else c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);
                    break;
                case FlexAlignItems.End:
                    if (IsRow) c.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right); else c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Bottom);
                    break;
                case FlexAlignItems.Center:
                case FlexAlignItems.Baseline:
                    if (IsRow) c.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center); else c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    break;
                case FlexAlignItems.Stretch:
                    if (IsRow) c.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch); else c.SetValue(VerticalAlignmentProperty, VerticalAlignment.Stretch);
                    break;
            }
        }

        public FlexDirection Direction { get => (FlexDirection)GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }
        public FlexWrap Wrap { get => (FlexWrap)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }
        public FlexJustifyContent JustifyContent { get => (FlexJustifyContent)GetValue(JustifyContentProperty); set => SetValue(JustifyContentProperty, value); }
        public FlexAlignItems AlignItems { get => (FlexAlignItems)GetValue(AlignItemsProperty); set => SetValue(AlignItemsProperty, value); }
        public FlexAlignContent AlignContent { get => (FlexAlignContent)GetValue(AlignContentProperty); set => SetValue(AlignContentProperty, value); }

        public static int GetOrder(DependencyObject obj) => (int)obj.GetValue(OrderProperty);
        public static void SetOrder(DependencyObject obj, int value) => obj.SetValue(OrderProperty, value);
        public static int GetGrow(DependencyObject obj) => (int)obj.GetValue(GrowProperty);
        public static void SetGrow(DependencyObject obj, int value) => obj.SetValue(GrowProperty, value);
        public static int GetShrink(DependencyObject obj) => (int)obj.GetValue(ShrinkProperty);
        public static void SetShrink(DependencyObject obj, int value) => obj.SetValue(ShrinkProperty, value);
        public static FlexAlignItems GetAlignSelf(DependencyObject obj) => (FlexAlignItems)obj.GetValue(AlignSelfProperty);
        public static void SetAlignSelf(DependencyObject obj, FlexAlignItems value) => obj.SetValue(AlignSelfProperty, value);
        public static double GetBasis(DependencyObject obj) => (double)obj.GetValue(BasisProperty);
        public static void SetBasis(DependencyObject obj, double value) => obj.SetValue(BasisProperty, value);
    }

    class ExtendedChild
    {
        public double min, max, current = -1, basis;
        public UIElement c;

        public ExtendedChild(UIElement e, double des, double totalBasis, DependencyProperty primaryaxsis, DependencyProperty primaryaxsismax, DependencyProperty primaryaxsisMin)
        {
            c = e;
            basis = Flexpanel.GetBasis(c) / totalBasis;

            max = (double)c.GetValue(primaryaxsis);

            if (double.IsNaN(max))
            {
                max = (double)c.GetValue(primaryaxsismax);

                if (double.IsNaN(max))
                    max = des;

                min = (double)c.GetValue(primaryaxsisMin);
                if (double.IsNaN(min))
                    min = des;
            }
            else
                min = max;
        }

        public double AddSpace(double available)
        {
            if (min == max || current == -1)
            {
                current = min;
                return current;
            }

            var remaining = max - current;

            if (available > remaining)
            {
                current = max;
                return remaining;
            }
            else
            {
                current += available;
                return available;
            }
        }

        public bool CanUseMoreSpace()
        {
            return current < max;
        }
    }

    public enum FlexAlignItems { NotSet, Start, End, Center, Baseline, Stretch }
    public enum FlexAlignContent { Start, End, Center, SpaceBetween, SpaceAround, Stretch }
    public enum FlexJustifyContent { Start, End, Center, SpaceBetween, SpaceAround }
    public enum FlexDirection { Row, RowReverse, Column, ColumnReverse }
    public enum FlexWrap { NoWrap, Wrap, WrapReverse }

}

