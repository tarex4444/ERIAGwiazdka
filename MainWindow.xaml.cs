using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Algorytm_A_gwiazdka
{
    public partial class MainWindow : Window
    {
        private const int CellSize = 20;
        public int[,] matrix;
        Node start = new Node(19, 0);
        Node end = new Node(0, 19);
        private bool isCtrlMode = false;
        public MainWindow()
        {
            InitializeComponent();
            matrix = ReadGridFromFile("gridempty.txt");
            DrawGrid(matrix);
            this.KeyDown += MainWindow_KeyDown;
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl && isCtrlMode == false)
            {
                isCtrlMode = true;
                SelectModeLabel.Content = "Tryb - wybór przeszkód";
                SelectModeLabel.Background = Brushes.LightGray;
            }
            else if (e.Key == Key.LeftCtrl && isCtrlMode == true)
            {
                isCtrlMode = false;
                SelectModeLabel.Content = "Tryb - wybór startu/finiszu";
                SelectModeLabel.Background = Brushes.LightBlue;
            }
        }

        static int[,] ReadGridFromFile(string fileName)
        {
            int[,] empty = { };
            try
            {                
                string[] lines = File.ReadAllLines(fileName);            
                var validLines = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                                      .Select(line => line.Split(' ')                
                                                          .Where(s => int.TryParse(s, out _)) 
                                                          .Select(int.Parse)                  
                                                          .ToArray())                         
                                      .ToList();
                int rowCount = validLines.Count;
                int colCount = validLines.Max(row => row.Length);
                int[,] result = new int[rowCount, colCount];
                for (int i = 0; i < rowCount; i++)
                {
                    for (int j = 0; j < validLines[i].Length; j++)
                    {
                        result[i, j] = validLines[i][j];
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd podczas odczytu pliku: {ex.Message}");
                return empty;
            }
        }

        private void DrawGrid(int[,] map)
        {
            PathCanvas.Children.Clear();

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(0); y++)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Black,
                        Fill = map[x,y] switch
                        {
                            5 => Brushes.DarkGray,
                            3 => Brushes.Green,
                            2 => Brushes.LimeGreen,
                            1 => Brushes.Red,
                            0 => Brushes.White
                        }
                    };
                    rect.Tag = new Point(x, y);
                    rect.MouseDown += Rectangle_MouseDown;
                    Canvas.SetLeft(rect, y * CellSize);
                    Canvas.SetTop(rect, x * CellSize);
                    PathCanvas.Children.Add(rect);
                }
            }
        }
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e) 
        {
            if (sender is Rectangle rect && rect.Tag is Point point)
            {
                int row = (int)point.X;
                int col = (int)point.Y;
                if (isCtrlMode)
                {                    
                    if(rect.Fill == Brushes.DarkGray)
                    {
                        matrix[row, col] = 0;
                        rect.Fill = Brushes.White;
                    } else
                    {
                        matrix[row, col] = 5;
                        rect.Fill = Brushes.DarkGray;
                    }
                } else
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        start.X = row;
                        start.Y = col;
                        foreach(var child in PathCanvas.Children)
                        {
                            if (child is Rectangle rectangle)
                            {
                                if (rectangle.Fill == Brushes.LightBlue)
                                {
                                    rectangle.Fill = Brushes.White;
                                }
                            }
                        }
                        rect.Fill = Brushes.LightBlue;
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        end.X = row;
                        end.Y = col;
                        foreach (var child in PathCanvas.Children)
                        {
                            if (child is Rectangle rectangle)
                            {
                                if (rectangle.Fill == Brushes.DarkBlue)
                                {
                                    rectangle.Fill = Brushes.White;
                                }
                            }
                        }
                        rect.Fill = Brushes.DarkBlue;
                    }
                    for (int i = 0; i < 20; i++)
                    {
                        for (int j = 0; j < 20; j++)
                        {
                            if (matrix[i, j] == 3)
                            {
                                matrix[i, j] = 0;
                            }
                        }
                    }
                }              
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "map_generator.exe",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    Thread.Sleep(300);
                    exeProcess.Kill();
                }
            }
            catch
            {
                MessageBox.Show("Nie można uruchomić generatora!", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //Process.Start("map_generator.exe");
            matrix = ReadGridFromFile("grid.txt");
            start.X = 19;
            start.Y = 0;
            end.X = 0;
            end.Y = 19;
            DrawGrid(matrix);
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            List<Node> path = FindPath(matrix, start, end);
            if (path != null)
            {
                foreach (var node in path)
                {
                    matrix[node.X, node.Y] = 3;
                    DrawGrid(matrix);
                }  
            } else
            {
                MessageBox.Show("Nie znaleziono ścieżki!","Brak możliwości znalezienia ścieżki.",MessageBoxButton.OK, MessageBoxImage.Error);
            }       
        }

        
        public class Node
        {
            public int X { get; set; }
            public int Y { get; set; }
            public double G { get; set; } 
            public double H { get; set; } 
            public double F => G + H; 
            public Node Parent { get; set; } 

            public Node(int x, int y)
            {
                X = x;
                Y = y;
                G = 0;
                H = 0;
                Parent = null;
            }
            public Node(int x, int y, Node parent)
            {
                X = x;
                Y = y;
                G = parent.G + 1;
                H = 0;
                Parent = parent;
            }

            public override bool Equals(object obj)
            {
                return obj is Node node && X == node.X && Y == node.Y;
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y);
            }
        }
        private static List<Node> GetNeighbors(Node node, int[,] grid)
        {
            var neighbors = new List<Node>();
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int newX = node.X + dx[i];
                int newY = node.Y + dy[i];

                if (newX >= 0 && newY >= 0 && newX < grid.GetLength(0) && newY < grid.GetLength(1) && grid[newX, newY] == 0)
                {
                    neighbors.Add(new Node(newX, newY, node));
                }
            }

            return neighbors;
        }

        private static double Heuristic(Node a, Node b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public List<Node> FindPath(int[,] grid, Node start, Node goal)
        {
            var greens = new List<Node>();
            var reds = new List<Node>();
            reds.Add(start);
            foreach (Node neighbor in GetNeighbors(start, grid))
            {
                neighbor.G = 1;
                neighbor.H = Heuristic(neighbor, end);
                greens.Add(neighbor);
            }
            //do getNeighbors dodać tworzenie Parenta i do node'a konstruktor z nim
            while (greens.Count > 0)
            {
                Node current = greens
                                .OrderByDescending(n => n.F)
                                //.ThenByDescending(n => greens.IndexOf(n))
                                .Last();

                if (current.Equals(goal))
                {
                    var path = new List<Node>();
                   while (current != null)
                    {
                        path.Add(current);
                        current = current.Parent;
                    }
                    return path;
                }
                greens.Remove(current);
                reds.Add(current);
                var neighbors = GetNeighbors(current, grid);
                for (int i = 0; i < neighbors.Count(); i++)
                {
                    if (reds.Any(n => n.Equals(neighbors[i])))
                    {
                        continue;
                    }
                    if (greens.Any(n => n.Equals(neighbors[i])) == false)
                    {
                        greens.Add(neighbors[i]);
                        neighbors[i].H = Heuristic(neighbors[i], end);
                        var neighborGValue = current.G + 1;
                        if (neighbors[i].F > neighborGValue + neighbors[i].H)
                        {
                            neighbors[i].Parent = current;
                            neighbors[i].G = neighborGValue;
                        }
                    }                   
                }
            }
            return null;
        }
    }
}
