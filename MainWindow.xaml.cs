using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Algorytm_A_gwiazdka
{
    public partial class MainWindow : Window
    {
        private char[][] matrix;
        private const int CellSize = 20;
        private int fromX = 0, fromY = 19, toX = 19, toY = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        static char[][] ReadGridFromFile(string fileName)
        {
            List<char[]> rows = new List<char[]>();

            try
            {
                foreach (string line in File.ReadLines(fileName))
                {
                    // Usunięcie spacji i zamiana na tablicę char[]
                    rows.Add(line.Replace(" ", "").ToCharArray());
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Błąd podczas wczytywania pliku: " + e.Message);
            }

            // Konwersja List<char[]> na char[][]
            return rows.ToArray();
        }

        private void DrawGrid()
        {
            PathCanvas.Children.Clear();

            for (int x = 0; x < matrix.Length; x++)
            {
                for (int y = 0; y < matrix[x].Length; y++)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Black,
                        Fill = matrix[x][y] switch
                        {
                            '5' => Brushes.DarkGray,
                            '3' => Brushes.LightBlue,
                            '0' => Brushes.White
                        }
                    };
                    Canvas.SetLeft(rect, y * CellSize);
                    Canvas.SetTop(rect, x * CellSize);
                    PathCanvas.Children.Add(rect);
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
            DrawGrid();
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            int fromXrb = fromX, fromYrb = fromY, toXrb = toX, toYrb = toY;
            matrixNode endNode = Agwiazdka(matrix, fromXrb, fromYrb, toXrb, toYrb);

            if (endNode == null || matrix[0][19] == '5' || matrix[19][0] == '5')
            {
                MessageBox.Show("Nie znaleziono ścieżki!");
                return;
            }

            Stack<matrixNode> path = new Stack<matrixNode>();
            while (endNode.x != fromXrb || endNode.y != fromYrb)
            {
                path.Push(endNode);
                endNode = endNode.parent;
            }
            path.Push(endNode);

            while (path.Count > 0)
            {
                var node = path.Pop();
                matrix[node.x][node.y] = '3';
            }

            DrawGrid();
        }

        //klasa dla węzła. Fr = koszt od węzła początkowego, to = heurystyka (odległość euklidesowa) sum= koszt, suma fr i to (zaimplementowane w funkcji Agwiazdka). X i Y to koordynaty, a parent wskazuje na węzeł poprzedzajacy (rodzica)
        public class matrixNode
        {
            public int fr = 0, to = 0, sum = 0;
            public int x, y;
            public matrixNode parent;
        }

        public static matrixNode Agwiazdka(char[][] matrix, int fromX, int fromY, int toX, int toY)
        {

            //Klucze dla zielonych(otwartych) i czerwonych(zamkniętych) to x.ToString() i y.ToString() matrixNode'a
            Dictionary<string, matrixNode> greens = new Dictionary<string, matrixNode>();
            Dictionary<string, matrixNode> reds = new Dictionary<string, matrixNode>();

            matrixNode startNode = new matrixNode { x = fromX, y = fromY }; //węzeł startowy
            string key = startNode.x.ToString() + startNode.y.ToString();
            greens.Add(key, startNode);
            //sprawdza nam węzeł z listy otwartej o najmniejszym koszcie (sum), żeby można było dalej z niego szukać. Przy równych wartościach, bierze tego o mniejszym kluczu (wcześniej napotkaną) 
            Func<KeyValuePair<string, matrixNode>> smallestGreen = () =>
            {
                KeyValuePair<string, matrixNode> smallest = greens.ElementAt(0);
                foreach (KeyValuePair<string, matrixNode> item in greens)
                {
                    if (item.Value.sum < smallest.Value.sum ||
                        (item.Value.sum == smallest.Value.sum && string.Compare(item.Key, smallest.Key, StringComparison.Ordinal) < 0))
                    {
                        smallest = item;
                    }
                }

                return smallest;
            };

            // Koordynaty
            List<KeyValuePair<int, int>> fourNeighbors = new List<KeyValuePair<int, int>>()
    {
        new KeyValuePair<int, int>(-1, 0), // góra
        new KeyValuePair<int, int>(1, 0),  // dół
        new KeyValuePair<int, int>(0, -1), // lewo
        new KeyValuePair<int, int>(0, 1)   // prawo
    };

            int maxX = matrix.GetLength(0);
            if (maxX == 0)
                return null;
            int maxY = matrix[0].Length;
            //magia działani algorytmu
            while (true)
            {
                if (greens.Count == 0)
                    return null;
                //sprawdza nam koodrynaty obecnego węzła
                KeyValuePair<string, matrixNode> current = smallestGreen();
                if (current.Value.x == toX && current.Value.y == toY)
                    return current.Value;

                greens.Remove(current.Key); //usuwa obecny węzeł z listy otwartej, bo już został rozważony do ekspansji
                reds.Add(current.Key, current.Value); //i dodaje go do listy zamkniętej aby na pewno go nie ruszać ponownie :)

                foreach (KeyValuePair<int, int> plusXY in fourNeighbors)
                {   //sprawdza nam koordynaty potencjalnych następnych ruchów 
                    int nbrX = current.Value.x + plusXY.Key;
                    int nbrY = current.Value.y + plusXY.Value;
                    string nbrKey = nbrX.ToString() + nbrY.ToString();
                    // sprawdza możliwość ruchu
                    if (nbrX < 0 || nbrY < 0 || nbrX >= maxX || nbrY >= maxY  //czy pola są na mapie do poruszania się
                        || matrix[nbrX][nbrY] == '5' // przeszkody
                        || reds.ContainsKey(nbrKey)) // węzły już odrzucone
                        continue;

                    if (greens.ContainsKey(nbrKey)) //jeśli jest w liście otwartej węzłów, sprawdza, czy jest on na optymalnej trasie
                    {
                        matrixNode curNbr = greens[nbrKey];
                        int newFr = current.Value.fr + 1; // koszt jednego ruchu
                        if (newFr < curNbr.fr)
                        {
                            curNbr.fr = newFr;
                            curNbr.sum = curNbr.fr + curNbr.to;
                            curNbr.parent = current.Value;
                        }
                    }
                    else
                    { //jeśli nie ma to sprawdza dystans od węzła startowego i dodaje nowy węzeł do listy otwartej
                        matrixNode curNbr = new matrixNode { x = nbrX, y = nbrY };
                        curNbr.fr = current.Value.fr + 1; // koszt jednego ruchu
                        curNbr.to = (int)Math.Sqrt(Math.Pow(nbrX - toX, 2) + Math.Pow(nbrY - toY, 2)); // dystans euklidesowy
                        curNbr.sum = curNbr.fr + curNbr.to;
                        curNbr.parent = current.Value;
                        greens.Add(nbrKey, curNbr);
                    }
                }
            }
        }
    }
}

