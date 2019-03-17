<Query Kind="Program" />

public Random seed = new Random();

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

	var width = 365;
	var height = 768;
	var histogram = GetProbabilityHistogram(results, width, height);
	
	var midpoint = Convert.ToInt32(results.Min(x => x.Delta));
	var allWalks = new List<Point>();
	for (int i = 0; i < 100; i++)
	{
		var walk = GenerateRandomWalk(histogram, midpoint, width, height);
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
}

private List<Point> GenerateRandomWalk(int[,] histogram, int midpoint, int width, int height)
{
	var walk = new List<Point>();
	
	for (var x = 0; x < width; x++)
	{
		int price = 0;
		while (price == 0)
		{
			// multiply all occurences for each price point, by the frequencies of the histogram
			// this gives a random distrobution against the probability dist. function
			var frequencies = new List<int>();
			for (int y = 0; y < height; y++)
			{
				var f = histogram[x,y];
				for (var j = 0; j < f; j++)
				{
					frequencies.Add(y);
				}
			}
		
			var i = seed.Next(0, frequencies.Count);
			price = i > -midpoint ? -frequencies[i] : frequencies[i];
		}

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

private int[,] GetProbabilityHistogram(List<Row> results, int width, int height)
{
	var total = new ProbabilityClassification(results, (x) => 0);
	var dayOfWeek = new ProbabilityClassification(results, (x) => (int)x.DayOfWeek, 5);
	var dayOfMonth = new ProbabilityClassification(results, (x) => (int)x.Day, 30);
	var monthOfYear = new ProbabilityClassification(results, (x) => (int)x.Month, 12);
	var dayOfYear = new ProbabilityClassification(results, (x) => (int)x.DayOfYear, 52);
	var allConditions = new[] { /*total,*/ dayOfWeek, dayOfMonth, /*monthOfYear,*/ dayOfYear };
	PopulateConditionals(results, allConditions);

	var date = DateTime.Today;
	var max = Convert.ToInt32(results.Max(x => x.Delta));
	var min = Convert.ToInt32(results.Min(x => x.Delta));
	var probability = new int[width,height];
	for (int x = 0; x < width; x++)
	{
		// Additively combine all histogram frequencies and means
		var d = date.AddDays(x);
		var average = 0;
		var frequencies = new Dictionary<int, int>();
		foreach (var c in allConditions)
		{
			average += c.GetMean(d);
			var histogram = c.GetHistogram(d);
			if (histogram == null)
				continue;
			foreach (var h in histogram)
			{
				if (frequencies.ContainsKey(h.Key))
					frequencies[h.Key] += h.Value;
				else
					frequencies.Add(h.Key, h.Value);
			}
		}
		average = average / allConditions.Length;
		
		for(int y = 0; y < height; y++)
		{
			if (frequencies.TryGetValue(y + min, out int frequency))
			{
				var offsetY = Math.Max(0, y/* + average*/);
				probability[x, offsetY] = frequency;
			}
		}
	}
	
	return probability;
}

private void PopulateConditionals(List<Row> values, ProbabilityClassification[] conditionals)
{
	foreach (var v in values)
	{
		foreach (var c in conditionals)
		{
			c.Add(v);
		}
	}
}

private class ProbabilityClassification
{
	private Func<DateTime, int> Conditional {get; set;}
	private Dictionary<int, Dictionary<int, int>> Histogram {get; set;}
	private Dictionary<int, int> Mean { get; set; }
	public int ScaleFactor { get; set;}
	
	public ProbabilityClassification(List<Row> values, Func<DateTime, int> condition, int scaleFactor = 1)
	{
		Conditional = condition;
		Histogram = new Dictionary<int, Dictionary<int, int>>();
		Mean = new Dictionary<int, int>();
		ScaleFactor = scaleFactor;
	}
	
	public void Add(Row value)
	{
		var key = (int)value.Delta;
		var classification = Conditional(value.Date);
		if (!Histogram.ContainsKey(classification))
			Histogram[classification] = new Dictionary<int, int> { { key, 1 } };
		else if (!Histogram[classification].ContainsKey(key))
			Histogram[classification].Add(key, 1);
		else
			Histogram[classification][key]++;

		// Re-evaluate mean for that classification
		var sum = Histogram[classification].Sum(x => x.Key * x.Value);
		var count = Histogram[classification].Values.Count();
		Mean[classification] = sum / count;
	}

	public Dictionary<int, int> GetHistogram(DateTime date)
	{
		var classification = Conditional(date);
		Histogram.TryGetValue(classification, out Dictionary<int, int> value);
		return value;
	}

	public int GetMean(DateTime date)
	{
		var classification = Conditional(date);
		Mean.TryGetValue(classification, out int value);
		return value;
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