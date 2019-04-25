<Query Kind="Program" />

public Random seed = new Random();

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

	var width = 365;
	var height = 768;
	var histogram = new Dictionary<int, Dictionary<int, int>>();
	for (int i = 1; i < 15; i++)
	{
		// for each price point, build a probability distrobution
		// looking back the previous 15 days worth of data
		GetSecondaryProbabilityHistogram(results, histogram, width, height, i);
	}
	
	var midpoint = 0;
	var allWalks = new List<Point>();
	for (int i = 0; i < 1; i++)
	{
		var walk = GenerateRandomWalk(histogram, midpoint, 3000, height);
		foreach (var w in walk)
		{
			w.Label = i;
		}
		allWalks.AddRange(walk);
	}

	var chart = allWalks.Select(x => x.X).Distinct().ToArray().Chart();
	foreach(var s in allWalks.GroupBy(x => x.Label))
	{
		var series = s.Select(x => x.Y).ToArray();
		chart.AddYSeries(series, LINQPad.Util.SeriesType.Line);
	}
	chart.Dump();

	// Chart the actual price data for comparison
	var j = 0;
	var test0 = results.Select(x => new Point{X = j++, Y = (int) x.Price}).ToList();
	test0.Chart(k => k.X, v => v.Y, LINQPad.Util.SeriesType.Line).Dump();
	j = 0;
	// Chart the actual autocorrelation for comparison
	var test1 = results.Select(x => new Point{X = j++, Y = (int) x.Delta}).ToList();
	var autocorreleation1 = WindowResults(test1);
	autocorreleation1.Chart(k => k.Key, v => v.Value).Dump();
	
	// Chart the generated datasets auto correlation
	var test = new List<Point>();
	for (int k = 1; k < 364; k++)
	{
		var delta = allWalks[k].Y - allWalks[k-1].Y;
		test.Add(new Point{X = k, Y = delta});
	}
	var autocorreleation = WindowResults(test);
	autocorreleation.Chart(k => k.Key, v => v.Value).Dump();
}

private Dictionary<int, double> WindowResults(List<Point> results)
{
	var output = new Dictionary<int, double>();

	for (int i = 1; i < 300; i++)
	{
		var past = new List<decimal>();
		var present = new List<decimal>();
		for (int j = i; j < results.Count; j++)
		{
			past.Add(Math.Abs( results[j-i].Y ));
			present.Add(Math.Abs( results[j].Y ));
		}
		
		var coefficient = ComputeCoefficient(present.ToArray(), past.ToArray());
		output.Add(i, coefficient);
	}
	return output;
}

private double ComputeCoefficient(decimal[] values1, decimal[] values2)
{
    if(values1.Length != values2.Length)
        throw new ArgumentException("values must be the same length");

    var avg1 = values1.Average();
    var avg2 = values2.Average();

    var sum1 = values1.Zip(values2, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

    var sumSqr1 = values1.Sum(x => Math.Pow((double)(x - avg1), 2.0));
    var sumSqr2 = values2.Sum(y => Math.Pow((double)(y - avg2), 2.0));

    var result = (double) sum1 / Math.Sqrt(sumSqr1 * sumSqr2);

    return result;
}

private List<Point> GenerateRandomWalk(Dictionary<int, Dictionary<int, int>> histogram, int midpoint, int width, int height)
{
	int price = midpoint;
	var walk = new List<Point>{new Point{X = 0, Y = 0}};
	for (var x = 1; x < width; x++)
	{
		// multiply all occurences for each price point, by the frequencies of the histogram
		// this gives a random distrobution against the probability dist. function
		var frequencies = new List<int>();
		foreach (var h in histogram[midpoint])
		{
			for (int f = 0; f < h.Value; f++)
			{
				frequencies.Add(h.Key);
			}
		}
	
		var i = seed.Next(0, frequencies.Count);
		//midpoint = Convert.ToInt32(frequencies.Count / 2);
		price = i > midpoint ? frequencies[i] : -frequencies[i];

		walk.Add(new Point
		{
			X = x,
			Y = x == 0 ? price : walk[x-1].Y + price
		});
	}
	
	return walk;
}

private class Point
{
	public int Label {get; set;}
	public int X {get; set;}
	public int Y {get; set;}
}

private void GetSecondaryProbabilityHistogram(
	List<Row> results, 
	Dictionary<int, Dictionary<int, int>> histogram,
	int width, 
	int height, 
	int windowSize)
{
	for (int i = windowSize; i < results.Count; i++)
	{
		var r = results[i-windowSize];		
		var key = Convert.ToInt32(r.Delta);
		var value = Convert.ToInt32(results[i].Delta);
		if (histogram.ContainsKey(key))
		{
			if (histogram[key].ContainsKey(value))
			{
				histogram[key][value]++;
			}
			else
			{
				histogram[key].Add(value, 1);
			}
		}
		else
		{
			histogram.Add(key, new Dictionary<int, int> { {value, 1} });
		}
	}
}

private List<Row> ReadOpeningPrices(string path)
{
	var results = new List<Row>();
	using (var reader = new StreamReader(path))
	{
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			var values = line.Split(',');

			var provider = System.Globalization.CultureInfo.InvariantCulture;
			var dateStr = values[0].ToString().Replace("\"", "");
			var date = DateTime.ParseExact(dateStr, "MMM-dd-yyyy", provider);

			var priceStr = values[1].ToString().Replace("\"", "");
			decimal price;
			if (!decimal.TryParse(priceStr, out price))
				continue;

			results.Add(new Row
			{
				Date = date,
				Price = price
			});
		}
	}
	results = results.OrderBy(x => x.Date).ToList();
	for (int i = 1; i < results.Count; i++)
	{
		var delta = results[i].Price - results[i - 1].Price;
		results[i].Delta = delta;
	}
	return results;
}

private class Row
{
	public DateTime Date {get; set;}
	public decimal Price {get; set;}
	public decimal Delta {get; set;}
}