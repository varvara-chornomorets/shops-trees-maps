using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

class Program
{
    static void Main()
    {
        // this makes encoding normal for varia
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Введіть широту: ");
        // CultureInfo makes no difference if it's 3.5 or 3,5
        double lat1 = Convert.ToDouble(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
        Console.WriteLine("Введіть довготу: ");
        double lon1 = Convert.ToDouble(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
        Console.WriteLine("Введіть радіус: ");
        double radius = Convert.ToDouble(Console.ReadLine().Replace(',', '.'), CultureInfo.InvariantCulture);
        
        Stopwatch sw = new Stopwatch();
        sw.Start();
        
        RTree tree = new RTree();
        tree.BuildTree();
        Console.WriteLine(tree);
        List<Point> result = tree.SearchPoints(lat1, lon1, radius);
        foreach (var p in result)
        {
            Console.WriteLine ($"lat is {p.Lat}, lon is {p.Lon}, {p.Type1}, {p.Type2}, {p.Name1}, {p.Name2}");
        }
        
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }
}

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
            // CultureInfo makes no difference if it's 3.5 or 3,5
            double lat = Convert.ToDouble(data[0].Replace(',', '.'), CultureInfo.InvariantCulture);
            double lon = Convert.ToDouble(data[1].Replace(',', '.'), CultureInfo.InvariantCulture);

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
            // CultureInfo makes no difference if it's 3.5 or 3,5
            double lat = Convert.ToDouble(data[0].Replace(',', '.'), CultureInfo.InvariantCulture);
            double lon = Convert.ToDouble(data[1].Replace(',', '.'), CultureInfo.InvariantCulture);
            string type1 = data[2];
            string type2 = data[3];
            string name1 = data[4];
            string name2 = data[5];

            rootNode.Insert(new Point(lat, lon, type1, type2, name1, name2));
        }

        rootNode.SplitRecursively();
    }

    public List<Point> SearchPoints(double lat, double lon, double radius)
    {
        // create rect node (actually square) 2r * 2r
        RectangularNode rect = new RectangularNode(lat-radius, lat+radius, lon-radius, lon+radius);
        List<Point> result = new List<Point>();
        SearchInNode(rootNode, rect, result, radius, lat, lon);
        return result;
    }

    private void SearchInNode(RectangularNode cur, RectangularNode toSearch, List<Point> result, double radius, double x, double y)
    {
        // if rects don't overlap we stop
        if (!Overlap(cur, toSearch))
        {
            return;
        }
        // if cur is a leaf we add its points to the result
        if ((cur.rightChild == null) && (cur.leftChild == null))
        {
            foreach (var p in cur.points)
            {
                if(p.Distance(x, y) <= radius)
                {
                    result.Add(p);
                }
            }
            return;
        }
        // if it isn't a leaf (and rects overlap) we continue searching
        SearchInNode(cur.leftChild, toSearch, result, radius, x, y);
        SearchInNode(cur.rightChild, toSearch, result, radius, x, y);
    }

    private bool Overlap(RectangularNode first, RectangularNode second)
    {
        // let l1 be top left point of the 1 rectangular, r1 - bottom right of the 1 rect
        // l2 left top of 2, r2 bottom right of 2
        SimplePoint l1 = new SimplePoint(first.minLat, first.maxLon);
        SimplePoint r1 = new SimplePoint(first.maxLat, first.minLon);
        SimplePoint l2 = new SimplePoint(second.minLat, second.maxLon);
        SimplePoint r2 = new SimplePoint(second.maxLat, second.minLon);
        
        // Two rectangles do not overlap if one of the following conditions is true. 
        // if One rectangle is above top edge of other rectangle. 
        if (r1.lon > l2.lon || r2.lon > l1.lon)
        {
            return false;
        }
        // if One rectangle is on left side of left edge of other rectangle.
        if (l1.lat > r2.lat || l2.lat > r1.lat)
        {
            return false;
        }

        return true;
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

class SimplePoint
{
    public double lat;
    public double lon;

    public SimplePoint(double lat, double lon)
    {
        this.lat = lat;
        this.lon = lon;
    }
}

class RectangularNode
{
    public double minLat;
    public double maxLat;
    public double minLon;
    public double maxLon;
    private Point point;
    public List<Point> points;
    public RectangularNode leftChild;
    public RectangularNode rightChild;

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

    public void SplitRecursively()
    {
        if (points.Count <= 100)
        {
            return;
        }

        RectangularNode left;
        RectangularNode right;
        if (maxLat - minLat >= maxLon - minLon)
        {
            (left, right) = SplitByLatitude();

        }
        else
        {
            (left, right) = SplitByLongitude();

        }
        leftChild = left;
        rightChild = right;
        leftChild.SplitRecursively();
        rightChild.SplitRecursively();

        this.points = null;
    }

    private (RectangularNode left, RectangularNode right) SplitByLatitude()
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

    private (RectangularNode left, RectangularNode right) SplitByLongitude()
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

}



// class SearchAreaPoint
// {
//     public double Lat { get; }
//     public double Lon { get; }
//     public string Type1 { get; }
//     public string Type2 { get; }
//     public string Name1 { get; }
//     public string Name2 { get; }
//
//     public SearchAreaPoint(double lat, double lon, string type1, string type2, string name1, string name2)
//     {
//         Lat = lat;
//         Lon = lon;
//         Type1 = type1;
//         Type2 = type2;
//         Name1 = name1;
//         Name2 = name2;
//     }
// }
