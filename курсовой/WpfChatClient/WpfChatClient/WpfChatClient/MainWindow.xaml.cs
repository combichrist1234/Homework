﻿using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SerializationTypes;
using Shape = System.Windows.Shapes.Shape;

namespace WpfChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal enum DrawType
    {
        Nothing,
        Line,
        Pen,
        Rectangle,
        Ellipse
    }
    public partial class MainWindow
    {
        private readonly TypeConverter _typeConverter;
        private readonly ServerHandler _serverHandler;
        private static string _userName;
        private DrawType _drawType;
        private Point _startPoint;
        private Rectangle _rect;
        private Ellipse _ellipse;
        private Shape _drawingShape;
        private const int Colums = 5;
        private Brush _selectedBrushColor;
        private int _lineThickness;
        
        public MainWindow()
        {
            InitializeComponent();
            _serverHandler = new ServerHandler("127.0.0.1", 8888,8889);
            ComboColors.ItemsSource = typeof(Brushes).GetProperties();
            _typeConverter = new BrushConverter();
        }
        private void table_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null) return;
            if (grid.RowDefinitions.Count == 0)
            {
                for (var r = 0; r <= ComboColors.Items.Count / Colums; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                }
            }
            if (grid.ColumnDefinitions.Count == 0)
            {
                for (var c = 0; c < Math.Min(ComboColors.Items.Count, Colums); c++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                }
            }
            for (var i = 0; i < grid.Children.Count; i++)
            {
                Grid.SetColumn(grid.Children[i], i % Colums);
                Grid.SetRow(grid.Children[i], i / Colums);
            }
        }
        private void sendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageText.Text;
            _serverHandler.SendMessage(message);
            MessageText.Clear();
            ChatBox.Dispatcher.Invoke(new Action<string>(s => ChatBox.AppendText(s)), _userName + ": " + message + Environment.NewLine);
        }
        private void confirmNicknameButton_Click(object sender, RoutedEventArgs e)
        {
            _userName = LocalName.Text;
            ChatBox.AppendText(_serverHandler.ConnectClient(_userName));
            ConfirmNicknameButton.IsEnabled = false;
            LocalName.IsEnabled = false;
            PaintSurface.IsEnabled = true;
            SendMessageButton.IsEnabled = true;
            var readMessage = new Task(ReadMessages);
            readMessage.Start();
            var readData=new Task(ReadData);
            readData.Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _serverHandler.CloseClientConnection();
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _drawingShape = null;
            switch (_drawType)
            {
                case DrawType.Nothing:
                    break;
                case DrawType.Line:
                    _startPoint = e.GetPosition(PaintSurface);
                    break;
                case DrawType.Pen:
                    _startPoint = e.GetPosition(PaintSurface);
                    break;
                case DrawType.Rectangle:
                    _startPoint = e.GetPosition(PaintSurface);
                    _rect = new Rectangle
                    {
                        Stroke = _selectedBrushColor,
                        StrokeThickness = _lineThickness
                    };
                    Canvas.SetLeft(_rect,_startPoint.X);
                    Canvas.SetTop(_rect,_startPoint.Y);
                    PaintSurface.Children.Add(_rect);
                    break;
                case DrawType.Ellipse:
                    _startPoint = e.GetPosition(PaintSurface);
                    _ellipse = new Ellipse
                    {
                        Stroke = _selectedBrushColor,
                        StrokeThickness = _lineThickness
                    };
                    Canvas.SetLeft(_ellipse,_startPoint.X);
                    Canvas.SetTop(_ellipse,_startPoint.Y);
                    PaintSurface.Children.Add(_ellipse);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _drawingShape = null;

            switch (_drawType)
            {
                case DrawType.Nothing:
                    break;
                case DrawType.Line:
                    var line = new Line
                    {
                        X1 = _startPoint.X,
                        Y1 = _startPoint.Y,
                        X2 = e.GetPosition(PaintSurface).X,
                        Y2 = e.GetPosition(PaintSurface).Y,
                        Stroke = _selectedBrushColor,
                        StrokeThickness = _lineThickness,
                    };
                    
                    PaintSurface.Children.Add(line);
                    var serializationLine = new SerializationLine(line.X1, line.Y1, line.X2, line.Y2, line.StrokeThickness, Convert.ToString(line.Stroke));
                    _serverHandler.SendData(serializationLine);
                    break;
                case DrawType.Pen:
                    break;
                case DrawType.Rectangle:
                    var serializationRectangle = new SerializationRectangle(_rect.StrokeThickness, Convert.ToString(_rect.Stroke), _rect.Width, _rect.Height, Canvas.GetLeft(_rect),Canvas.GetTop(_rect));
                    _serverHandler.SendData(serializationRectangle);
                    _rect = null;
                    break;
                case DrawType.Ellipse:
                    var serializationEllipse = new SerializationEllipse(_ellipse.StrokeThickness, Convert.ToString(_ellipse.Stroke), _ellipse.Width, _ellipse.Height, Canvas.GetLeft(_ellipse),Canvas.GetTop(_ellipse));
                    _serverHandler.SendData(serializationEllipse);
                    _ellipse = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }        
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            switch (_drawType)
            {
                case DrawType.Nothing:
                    break;
                case DrawType.Line:
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        if (_drawingShape is Line)
                        {
                            PaintSurface.Children.Remove(_drawingShape);
                        }

                        var line = new Line
                        {
                            X1 = _startPoint.X,
                            Y1 = _startPoint.Y,
                            X2 = e.GetPosition(PaintSurface).X,
                            Y2 = e.GetPosition(PaintSurface).Y,
                            Stroke = _selectedBrushColor,
                            StrokeThickness = _lineThickness
                        };


                        _drawingShape = line;
                        PaintSurface.Children.Add(line);
                    }
                    break;
                case DrawType.Pen:
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        var drawPan = new Line
                        {
                            Stroke = _selectedBrushColor,
                            X1 = _startPoint.X,
                            Y1 = _startPoint.Y,
                            X2 = e.GetPosition(PaintSurface).X,
                            Y2 = e.GetPosition(PaintSurface).Y,
                            StrokeThickness = _lineThickness
                        };
                        _startPoint = e.GetPosition(PaintSurface);
                        PaintSurface.Children.Add(drawPan);
                        var serializationLine = new SerializationLine(drawPan.X1, drawPan.Y1, drawPan.X2, drawPan.Y2, drawPan.StrokeThickness, Convert.ToString(drawPan.Stroke));
                        _serverHandler.SendData(serializationLine);
                    }
                    break;
                case DrawType.Rectangle:
                    if (e.LeftButton == MouseButtonState.Pressed) 
                    { 
                    var pos = e.GetPosition(PaintSurface);

                    var x = Math.Min(pos.X, _startPoint.X);
                    var y = Math.Min(pos.Y, _startPoint.Y);

                    var w = Math.Max(pos.X, _startPoint.X) - x;
                    var h = Math.Max(pos.Y, _startPoint.Y) - y;

                    _rect.Width = w;
                    _rect.Height = h;

                    Canvas.SetLeft(_rect, x);
                    Canvas.SetTop(_rect, y);
                    }
                    break;
                case DrawType.Ellipse:
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        var pos = e.GetPosition(PaintSurface);

                        var x = Math.Min(pos.X, _startPoint.X);
                        var y = Math.Min(pos.Y, _startPoint.Y);

                        var w = Math.Max(pos.X, _startPoint.X) - x;
                        var h = Math.Max(pos.Y, _startPoint.Y) - y;

                        _ellipse.Width = w;
                        _ellipse.Height = h;

                        Canvas.SetLeft(_ellipse, x);
                        Canvas.SetTop(_ellipse, y);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LineButton_Checked(object sender, RoutedEventArgs e)
        {
            _drawType=DrawType.Line;
            PenButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            EllipseButton.IsChecked = false;
        }

        private void PenButton_Checked(object sender, RoutedEventArgs e)
        {
            _drawType=DrawType.Pen;
            LineButton.IsChecked = false;
            RectangleButton.IsChecked = false;
            EllipseButton.IsChecked = false;
        }
        private void RectangleButton_Checked(object sender, RoutedEventArgs e)
        {
            _drawType = DrawType.Rectangle;
            LineButton.IsChecked = false;
            PenButton.IsChecked = false;
            EllipseButton.IsChecked = false;
        }
        private void EllipseButton_Checked(object sender, RoutedEventArgs e)
        {
            _drawType = DrawType.Ellipse;
            LineButton.IsChecked = false;
            PenButton.IsChecked = false;
            RectangleButton.IsChecked = false;
        }
        private void comboColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var colorName = Convert.ToString(ComboColors.SelectedItem).Replace(typeof(SolidColorBrush).FullName, string.Empty);
            var brushColor = _typeConverter.ConvertFromString(colorName) as Brush;
            if (brushColor != null)
            {
                _selectedBrushColor = brushColor;
            }
            else
            {
                throw new InvalidCastException(string.Format("Cannot convert to type {0}", typeof(Brush).FullName));
            }
        }

        private void lineWidthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _lineThickness = lineWidthComboBox.SelectedIndex+1;
        }
        private void ReadMessages()
        {
            while (true)
            {
                if (_serverHandler == null)
                    return;
                var result = _serverHandler.ReceiveMessage();
                ChatBox.Dispatcher.Invoke(new Action<string>(s => ChatBox.AppendText(s)), result + Environment.NewLine);//вывод сообщения
            }
        }
        private void ReadData()
        {
            while (true)
            {
                if (_serverHandler == null)
                    return;
                var result = _serverHandler.ReceiveData();

                if (result is SerializationLine)
                {
                    var resultLine = result as SerializationLine;

                    PaintSurface.Dispatcher.Invoke(new Action(() =>
                    {
                        var line = new Line
                        {
                            X1 = resultLine.StartPositionX,
                            Y1 = resultLine.StartPositionY,
                            X2 = resultLine.EndPositionX,
                            Y2 = resultLine.EndPositionY,
                            Stroke = (Brush)_typeConverter.ConvertFromString(resultLine.Color),
                            StrokeThickness = resultLine.Size,
                        };
                        PaintSurface.Children.Add(line);
                    }));
                }
                else if (result is SerializationEllipse)
                {
                    var resultEllipse = result as SerializationEllipse;

                    PaintSurface.Dispatcher.Invoke(new Action(() =>
                    {
                        _ellipse = new Ellipse
                        {
                            Stroke = (Brush)_typeConverter.ConvertFromString(resultEllipse.Color),
                            StrokeThickness = resultEllipse.Size,
                            Height=resultEllipse.Height,
                            Width=resultEllipse.Width,
                        };

                        Canvas.SetLeft(_ellipse,resultEllipse.Left);
                        Canvas.SetTop(_ellipse, resultEllipse.Top);
                        PaintSurface.Children.Add(_ellipse);
                    }));
                }
                else if (result is SerializationRectangle)
                {
                    var resultRectangle = result as SerializationRectangle;

                    PaintSurface.Dispatcher.Invoke(new Action(() =>
                    {
                        _rect = new Rectangle
                        {
                            Stroke = (Brush)_typeConverter.ConvertFromString(resultRectangle.Color),
                            StrokeThickness = resultRectangle.Size,
                            Height = resultRectangle.Height,
                            Width = resultRectangle.Width,
                        };

                        Canvas.SetLeft(_rect, resultRectangle.Left);
                        Canvas.SetTop(_rect, resultRectangle.Top);
                        PaintSurface.Children.Add(_rect);
                    }));
                }
            }
        }
    }
}
