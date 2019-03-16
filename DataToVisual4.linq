<Query Kind="Program" />

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

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
	var bitmap = new System.Drawing.Bitmap(365 + 2, 768);
	for (int x = 0; x < bitmap.Width - 2; x++)
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
		
		for(int y = 0; y < bitmap.Height; y++)
		{
			if (frequencies.TryGetValue(y + min, out int frequency))
			{
				var offsetY = Math.Max(0, y + average);
				var intensity = Math.Min(frequency * 20, 255);
				
				// Gradient
//				var colour = System.Drawing.Color.FromArgb(0, intensity, 0);
				
				// Colour scale
				float input = ((float)Math.Min(max, intensity) / (float)max);
				var colour = RainbowNumberToColor(input);
				
				bitmap.SetPixel(x, offsetY, colour);
			}
		}
	}

	// colour scale
	for (int value = 0; value < bitmap.Height; value++)
	{
		float input = ((float)value / (float)bitmap.Height);
		var color = RainbowNumberToColor(input);
		bitmap.SetPixel(bitmap.Width - 1, value, color);
		bitmap.SetPixel(bitmap.Width - 2, value, color);
	}

	//bitmap = ResizeImage(bitmap, bitmap.Width * 2, bitmap.Height);
	var output = ImageToByte(bitmap);
	Util.Image(output).Dump();
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

public static byte[] ImageToByte(System.Drawing.Image img)
{
	using (var stream = new MemoryStream())
	{
		img.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
		return stream.ToArray();
	}
}

public static System.Drawing.Color RainbowNumberToColor(float number)
{
	byte r = 0, g = 0, b = 0;
	var divisor = 4f;

	if (number < 1 / divisor)
	{
		// Mostly red with some green.
		r = 255;
		g = (byte)(r * (number - 0) / (2 / divisor - number));
	}
	else if (number < 2 / divisor)
	{
		// Mostly green with some red.
		g = 255;
		r = (byte)(g * (2 / divisor - number) / (number - 0));
	}
	else if (number < 3 / divisor)
	{
		// Mostly green with some blue.
		g = 255;
		b = (byte)(g * (2 / divisor - number) / (number - 4 / divisor));
	}
	else 
	{
		// Mostly blue with some green.
		b = 255;
		g = (byte)(b * (number - 4 / divisor) / (2 / divisor - number));
	}

	return System.Drawing.Color.FromArgb(r, g, b);
}

public static System.Drawing.Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
{
    var destRect = new System.Drawing.Rectangle(0, 0, width, height);
    var destImage = new System.Drawing.Bitmap(width, height);

    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

    using (var graphics = System.Drawing.Graphics.FromImage(destImage))
    {
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
        {
            wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width,image.Height, System.Drawing.GraphicsUnit.Pixel, wrapMode);
        }
    }

    return destImage;
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