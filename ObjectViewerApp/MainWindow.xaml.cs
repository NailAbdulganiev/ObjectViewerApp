using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using OfficeOpenXml;

namespace ObjectViewerApp
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ObjectData> Objects { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Objects = new ObservableCollection<ObjectData>();
            DataGridObjects.ItemsSource = Objects;

            this.Loaded += (s, e) => DrawCoordinateSystem();
            this.SizeChanged += CanvasDisplay_SizeChanged;
        }

        private void ImportFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx|CSV Files|*.csv",
                Title = "Выберите файл для импорта"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                if (System.IO.Path.GetExtension(filePath).ToLower() == ".csv")
                {
                    ImportCsv(filePath);
                }
                else
                {
                    ImportExcel(filePath);
                }
            }
        }

        private void ImportExcel(string filePath)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        double x = Convert.ToDouble(worksheet.Cells[row, 2].Text);
                        double y = Convert.ToDouble(worksheet.Cells[row, 3].Text);
                        double width = Convert.ToDouble(worksheet.Cells[row, 4].Text);
                        double height = Convert.ToDouble(worksheet.Cells[row, 5].Text);

                        if (x < 0 || x > 40 || y < 0 || y > 24 || width < 0 || width > 40 || height < 0 || height > 24)
                        {
                            MessageBox.Show($"Ошибка в файле Excel: Один из параметров выходит за допустимый диапазон. Строка: {row}",
                                "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }

                        Objects.Add(new ObjectData
                        {
                            Name = worksheet.Cells[row, 1].Text,
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height,
                            IsDefect = worksheet.Cells[row, 6].Text.ToLower() == "yes"
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка в файле Excel: {ex.Message}", "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ImportCsv(string filePath)
        {
            var line = File.ReadAllLines(filePath);
            for (int i = 1; i < line.Length; i++)
            {
                var parts = line[i].Split(';');
                if (parts.Length == 6)
                {
                    try
                    {
                        double x = Convert.ToDouble(parts[1]);
                        double y = Convert.ToDouble(parts[2]);
                        double width = Convert.ToDouble(parts[3]);
                        double height = Convert.ToDouble(parts[4]);

                        if (x < 0 || x > 40 || y < 0 || y > 24 || width < 0 || width > 40 || height < 0 || height > 24)
                        {
                            MessageBox.Show($"Ошибка в файле CSV: Один из параметров выходит за допустимый диапазон. Строка: {line}",
                                "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }

                        Objects.Add(new ObjectData
                        {
                            Name = parts[0],
                            X = x,
                            Y = y,
                            Width = width,
                            Height = height,
                            IsDefect = parts[5].ToLower() == "yes"
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибки в файле CSV: {ex.Message}", "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DataGridObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridObjects.SelectedItem is ObjectData selectedObject)
            {
                InformationBlock.Text = $"Название: {selectedObject.Name}\nX: {selectedObject.X} м\nY: {selectedObject.Y} ч\n" +
                         $"Ширина: {selectedObject.Width} м\nВысота: {selectedObject.Height} ч\n" +
                         $"Дефект: {(selectedObject.IsDefect ? "Да" : "Нет")}";
                DrawObjectOnCanvas(selectedObject);
            }
        }

        private void CanvasDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoordinateCanvas.Children.Clear();
            DrawCoordinateSystem();

            if (DataGridObjects.SelectedItem is ObjectData selectedObject)
                DrawObjectOnCanvas(selectedObject);
        }

        private void DrawCoordinateSystem()
        {
            double canvasWidth = CoordinateCanvas.ActualWidth;
            double canvasHeight = CoordinateCanvas.ActualHeight;

            // Ось X
            var xAxis = new Line
            {
                X1 = 0,
                Y1 = canvasHeight,
                X2 = canvasWidth,
                Y2 = canvasHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            CoordinateCanvas.Children.Add(xAxis);

            // Ось Y
            var yAxis = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = canvasHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            CoordinateCanvas.Children.Add(yAxis);

            // Метки на оси X
            for (int i = 0; i <= 40; i++)
            {
                double xPosition = i * canvasWidth / 40;

                var tick = new Line
                {
                    X1 = xPosition,
                    Y1 = canvasHeight - 5,
                    X2 = xPosition,
                    Y2 = canvasHeight + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                CoordinateCanvas.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 12
                };
                Canvas.SetLeft(label, xPosition - 10);
                Canvas.SetTop(label, canvasHeight + 5);
                CoordinateCanvas.Children.Add(label);
            }

            // Метки на оси Y
            for (int i = 0; i <= 24; i++)
            {
                double yPosition = canvasHeight - (i * canvasHeight / 24);

                var tick = new Line
                {
                    X1 = -5,
                    Y1 = yPosition,
                    X2 = 5,
                    Y2 = yPosition,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                CoordinateCanvas.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 12
                };
                Canvas.SetLeft(label, -25);
                Canvas.SetTop(label, yPosition - 10);
                CoordinateCanvas.Children.Add(label);
            }
        }

        private void DrawObjectOnCanvas(ObjectData selectedObject)
        {
            ObjectCanvas.Children.Clear();
            double canvasWidth = ObjectCanvas.ActualWidth;
            double canvasHeight = ObjectCanvas.ActualHeight;

            double scaleX = canvasWidth / 40;
            double scaleY = canvasHeight / 24;
            if (selectedObject.X + selectedObject.Width > 40)
            {
                MessageBox.Show($"Ошибка в значениях продукта '{selectedObject.Name}'. " +
                    $"Отрисовка данного объекта выйдет за пределы координатной плоскости по оси X.",
                            "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (selectedObject.Y + selectedObject.Height > 24)
            {
                MessageBox.Show($"Ошибка в значениях продукта '{selectedObject.Name}'. " +
                   $"Отрисовка данного объекта выйдет за пределы координатной плоскости по оси Y.",
                           "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var rect = new Rectangle
                {
                    Width = selectedObject.Width * scaleX,
                    Height = selectedObject.Height * scaleY,
                    Stroke = Brushes.Indigo,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(rect, selectedObject.X * scaleX);
                Canvas.SetTop(rect, canvasHeight - (selectedObject.Y * scaleY) - rect.Height);

                ObjectCanvas.Children.Add(rect);
            }
        }

        public class ObjectData
        {
            public string Name { get; set; } = string.Empty;
            public double X { get; set; } = 0;
            public double Y { get; set; } = 0;
            public double Width { get; set; } = 0;
            public double Height { get; set; } = 0;
            public bool IsDefect { get; set; } = false;
        }
    }
}
