<Query Kind="Program" />

void Main()
{
	var path = @"C:\Users\Ben\Documents\Shares\au.investing\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);
	
	var dailyHistogram = GetHistogram(results);
	var monthlyHistogram = GetHistogram(GroupByMonth(results));
	//var weeklyHistogram = GetHistogram(GroupByWeek(results));
	
	var histogram = CombineFrequencies(dailyHistogram/*, weeklyHistogram, monthlyHistogram*/);
	
//	results.Chart(x => x.Date, v => v.Price).Dump();
//	histogram.Chart(k => k.Key, v => v.Value).Dump();

	var width = 365 * 3;
	var height = histogram.Max(x => x.Key);
	var image = new System.Drawing.Bitmap(width, height);
	for (int x = 0; x < width; x++)
	{
		for(int y = 0; y < height; y++)
		{
			if (histogram.TryGetValue(y, out int frequency))
			{
				var colour = System.Drawing.Color.FromArgb(0, frequency * 4, 0);
				image.SetPixel(x, y, colour);
			}
		}
	}
	Util.Image(BitmapDataFromBitmap(image, System.Drawing.Imaging.ImageFormat.Bmp)).Dump();
}

private Dictionary<int, int> CombineFrequencies(params Dictionary<int, int>[] histograms)
{
	// Given multiple histograms
	// histograms range from -X to +X, shift them to centre around 0
	// add frequencies together

	var results = new Dictionary<int, int>();
	
	var min = histograms.SelectMany(x => x.Keys).Min();
	foreach(var h in histograms)
	{
		foreach (var d in h)
		{
			var key = d.Key - min;
			if (results.ContainsKey(key))
			{
				results[key] += d.Value;
			}
			else
			{
				results.Add(key, d.Value);
			}
		}
	}
	return results;
}

//private List<Row> GroupByDays(List<Row> results, int days)
//{
//	var values = new List<Row>(365);
//	int i = 0;
//	foreach (var g in results.Skip(i * days).Take(days))
//	{
//		i++;
//		var mean = g.Sum(x => x.Price) / g.Count();
//		values.Add(new Row { Date = g.First().Date, Price = mean});
//	}
//	return values;
//}

private List<Row> GroupByMonth(List<Row> results)
{
	var values = new List<Row>(365);
	var y = results.GroupBy(x => x.Date.Month).ToList();
	foreach (var g in y)
	{
		var mean = g.Sum(x => x.Price) / g.Count();
		values.Add(new Row { Date = g.First().Date, Price = mean});
	}
	return values;
}

private Dictionary<int, int> GetHistogram(List<Row> results)
{
	var histogram = new Dictionary<int, int>();
	for (int i = 1; i < results.Count; i++)
	{
		var delta = results[i - 1].Price - results[i].Price;
		
		var key = Convert.ToInt32(delta);
		if (histogram.ContainsKey(key))
			histogram[key]++;
		else
			histogram.Add(key, 1);
	}
	return histogram;
}

private byte[] BitmapDataFromBitmap(System.Drawing.Bitmap objBitmap, System.Drawing.Imaging.ImageFormat imageFormat)

{
	using(MemoryStream ms = new MemoryStream())
	{
		objBitmap.Save(ms, imageFormat);
		return (ms.GetBuffer());
	}
}

private List<Row> ReadOpeningPrices(string path)
{
	var results = new List<Row>();
	using(var reader = new StreamReader(path))
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
	return results.OrderBy(x => x.Date).ToList();
}

public class Row
{
	public DateTime Date {get; set;}
	public decimal Price {get; set;}
	public decimal Delta {get; set;}
}