using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GhostClip;

public partial class SnippingOverlay : Window
{
    private readonly BitmapSource _desktopImage;

    private Point? _startPoint;

    public BitmapSource? SelectedImage { get; private set; }

    public SnippingOverlay(
        BitmapSource desktopImage,
        double left,
        double top,
        double width,
        double height)
    {
        InitializeComponent();

        _desktopImage = desktopImage;

        DesktopImage.Source = desktopImage;

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);

        SelectionRectangle.Visibility = Visibility.Visible;

        Canvas.SetLeft(
            SelectionRectangle,
            _startPoint.Value.X);

        Canvas.SetTop(
            SelectionRectangle,
            _startPoint.Value.Y);

        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;

        CaptureMouse();
    }

    private void Window_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (_startPoint is null ||
            e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current =
            e.GetPosition(SelectionCanvas);

        UpdateSelectionRectangle(
            _startPoint.Value,
            current);
    }

    private void Window_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_startPoint is null)
        {
            return;
        }

        Point endPoint =
            e.GetPosition(SelectionCanvas);

        Point startPoint =
            _startPoint.Value;

        _startPoint = null;

        ReleaseMouseCapture();

        Rect selection =
            CreateNormalisedRect(
                startPoint,
                endPoint);

        if (selection.Width < 3 ||
            selection.Height < 3)
        {
            SelectionRectangle.Visibility =
                Visibility.Collapsed;

            return;
        }

        double scaleX =
            _desktopImage.PixelWidth /
            ActualWidth;

        double scaleY =
            _desktopImage.PixelHeight /
            ActualHeight;

        int x =
            Math.Clamp(
                (int)Math.Floor(
                    selection.X * scaleX),
                0,
                _desktopImage.PixelWidth - 1);

        int y =
            Math.Clamp(
                (int)Math.Floor(
                    selection.Y * scaleY),
                0,
                _desktopImage.PixelHeight - 1);

        int width =
            Math.Clamp(
                (int)Math.Ceiling(
                    selection.Width * scaleX),
                1,
                _desktopImage.PixelWidth - x);

        int height =
            Math.Clamp(
                (int)Math.Ceiling(
                    selection.Height * scaleY),
                1,
                _desktopImage.PixelHeight - y);

        CroppedBitmap cropped =
            new(
                _desktopImage,
                new Int32Rect(
                    x,
                    y,
                    width,
                    height));

        cropped.Freeze();

        SelectedImage = cropped;

        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        DialogResult = false;
        Close();
    }

    private void UpdateSelectionRectangle(
        Point start,
        Point current)
    {
        Rect rect =
            CreateNormalisedRect(
                start,
                current);

        Canvas.SetLeft(
            SelectionRectangle,
            rect.Left);

        Canvas.SetTop(
            SelectionRectangle,
            rect.Top);

        SelectionRectangle.Width =
            rect.Width;

        SelectionRectangle.Height =
            rect.Height;
    }

    private static Rect CreateNormalisedRect(
        Point first,
        Point second)
    {
        double left =
            Math.Min(first.X, second.X);

        double top =
            Math.Min(first.Y, second.Y);

        double right =
            Math.Max(first.X, second.X);

        double bottom =
            Math.Max(first.Y, second.Y);

        return new Rect(
            left,
            top,
            right - left,
            bottom - top);
    }
}
