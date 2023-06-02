using System.Diagnostics;
using System.Globalization;




static void Main()
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("Введіть широту: ");
    double lat1 = Convert.ToDouble(Console.ReadLine(), new CultureInfo("de-DE"));
    Console.WriteLine("Введіть довготу: ");
    double lon1 = Convert.ToDouble(Console.ReadLine(), new CultureInfo("de-DE"));
    Console.WriteLine("Введіть радіус: ");
    double radius = Convert.ToDouble(Console.ReadLine(), new CultureInfo("de-DE"));
    Stopwatch sw = new Stopwatch();
    sw.Start();

    string[] lines = File.ReadAllLines("data.csv");
    for (int i = 0; i < lines.Length; i++)
    {
        string[] data = lines[i].Split(';');
        double lat2 = Convert.ToDouble(data[0], new CultureInfo("de-DE"));
        double lon2 = Convert.ToDouble(data[1], new CultureInfo("de-DE"));
        string type1 = data[2];
        string type2 = data[3];
        string name1 = data[4];
        string name2 = data[5];

        double distance = HaversineDistance(lat1, lon1, lat2, lon2);
        if (distance <= radius)
        {
            Console.WriteLine("{0} {1} {2} {3} Haversine: {4}", type1, type2, name1, name2, distance);
        }
    }
    sw.Stop();
    Console.WriteLine($"Elapsed time: {sw.Elapsed}");
}

static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
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

static double ToRadians(double angle)
{
    return Math.PI * angle / 180.0;
}

Main();

