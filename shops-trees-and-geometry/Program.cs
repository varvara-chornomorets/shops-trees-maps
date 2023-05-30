using System.Diagnostics;

static void Main()
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("Введіть широту: ");
    double lat1 = Convert.ToDouble(Console.ReadLine());
    Console.WriteLine("Введіть довготу: ");
    double lon1 = Convert.ToDouble(Console.ReadLine());
    Console.WriteLine("Введіть радіус: ");
    double radius = Convert.ToDouble(Console.ReadLine());

    Stopwatch sw = new Stopwatch();
    sw.Start();

    RTree tree = new RTree();
    tree.BuildTree();

    var results = tree.SearchPoints(lat1, lon1, radius);
    foreach (var result in results)
    {
        Console.WriteLine("{0} {1} {2} {3} Haversine: {4}", result.Type1, result.Type2, result.Name1, result.Name2, result.Distance);
    }

    sw.Stop();
    Console.WriteLine(sw.Elapsed);
}

Main();

class RTree
{
    private RectangularNode rootNode;

    public void BuildTree()
    {
        string[] lines = File.ReadAllLines("data.csv");
        double minLat = double.MaxValue;
        double maxLat = double.MinValue;
        double minLon = double.MaxValue;
        double maxLon = double.MinValue;

        foreach (string line in lines)
        {
            string[] data = line.Split(';');
            double lat = Convert.ToDouble(data[0]);
            double lon = Convert.ToDouble(data[1]);

            if (lat < minLat)
                minLat = lat;
            if (lat > maxLat)
                maxLat = lat;
            if (lon < minLon)
                minLon = lon;
            if (lon > maxLon)
                maxLon = lon;
        }

        rootNode = new RectangularNode(minLat, maxLat, minLon, maxLon);

        foreach (string line in lines)
        {
            string[] data = line.Split(';');
            double lat = Convert.ToDouble(data[0]);
            double lon = Convert.ToDouble(data[1]);
            string type1 = data[2];
            string type2 = data[3];
            string name1 = data[4];
            string name2 = data[5];

            rootNode.Insert(new Point(lat, lon, type1, type2, name1, name2));
        }
    }

    public SearchAreaPoint[] SearchPoints(double lat, double lon, double radius)
    {
        return rootNode.Search(lat, lon, radius);
    }
}

class Point
{
    public double Lat { get; }
    public double Lon { get; }
    public string Type1 { get; }
    public string Type2 { get; }
    public string Name1 { get; }
    public string Name2 { get; }

    public Point(double lat, double lon, string type1, string type2, string name1, string name2)
    {
        Lat = lat;
        Lon = lon;
        Type1 = type1;
        Type2 = type2;
        Name1 = name1;
        Name2 = name2;
    }

    public double Distance(double lat, double lon)
    {
        return HaversineDistance(Lat, Lon, lat, lon);
    }

    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        lat1 = ToRadians(lat1);
        lat2 = ToRadians(lat2);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        double d = 2 * R * Math.Asin(Math.Sqrt(a));
        return d;
    }

    private double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}

class RectangularNode
{
    private double minLat;
    private double maxLat;
    private double minLon;
    private double maxLon;
    private Point point;
    private List<Point> points;
    private RectangularNode leftChild;
    private RectangularNode rightChild;

    public RectangularNode(double minLat, double maxLat, double minLon, double maxLon)
    {
        this.minLat = minLat;
        this.maxLat = maxLat;
        this.minLon = minLon;
        this.maxLon = maxLon;
        points = new List<Point>();
    }

    public void Insert(Point p)
    {
        points.Add(p);
    }

    public (RectangularNode left, RectangularNode right) SplitByLatitude()
    {
        double medianLat = FindMedian(points, p => p.Lat);
        var left = new RectangularNode(minLat, medianLat, minLon, maxLon);
        var right = new RectangularNode(medianLat, maxLat, minLon, maxLon);

        foreach (Point p in points)
        {
            if (p.Lat <= medianLat)
                left.Insert(p);
            else
                right.Insert(p);
        }
        return (left, right);
    }

    public (RectangularNode left, RectangularNode right) SplitByLongtitude()
    {
        double medianLon = FindMedian(points, p => p.Lon);
        var left = new RectangularNode(minLat, maxLat, minLon, medianLon);
        var right = new RectangularNode(minLat, maxLat, medianLon, maxLon);

        foreach (Point p in points)
        {
            if (p.Lon <= medianLon)
                left.Insert(p);
            else
                right.Insert(p);
        }
        return (left, right);
    }

    private double FindMedian(List<Point> points, Func<Point, double> selector)
    {
        List<double> values = new List<double>();
        foreach (Point p in points)
        {
            values.Add(selector(p));
        }

        values.Sort();
        int count = values.Count;
        if (count % 2 == 0)
        {
            int midIndex1 = count / 2 - 1;
            int midIndex2 = count / 2;
            return (values[midIndex1] + values[midIndex2]) / 2;
        }
        else
        {
            int midIndex = count / 2;
            return values[midIndex];
        }
    }

    public SearchAreaPoint[] Search(double lat, double lon, double radius)
    {
        List<SearchAreaPoint> searchResults = new List<SearchAreaPoint>();

        foreach (Point p in points)
        {
            double distance = p.Distance(lat, lon);

            if (distance <= radius)
            {
                SearchAreaPoint searchResult = new SearchAreaPoint(p.Type1, p.Type2, p.Name1, p.Name2, distance);
                searchResults.Add(searchResult);
            }
        }

        return searchResults.ToArray();
    }
}

class SearchAreaPoint
{
    public string Type1 { get; }
    public string Type2 { get; }
    public string Name1 { get; }
    public string Name2 { get; }
    public double Distance { get; }

    public SearchAreaPoint(string type1, string type2, string name1, string name2, double distance)
    {
        Type1 = type1;
        Type2 = type2;
        Name1 = name1;
        Name2 = name2;
        Distance = distance;
    }
}
