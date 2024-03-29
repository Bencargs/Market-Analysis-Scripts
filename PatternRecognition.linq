<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.DataVisualization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\WindowsBase.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Xaml.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\UIAutomationTypes.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\System.Windows.Input.Manipulations.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\UIAutomationProvider.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Deployment.dll</Reference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Windows.Media.Imaging</Namespace>
  <Namespace>System.Windows</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
</Query>

[System.Runtime.InteropServices.DllImport("gdi32.dll")]
public static extern bool DeleteObject(IntPtr hObject);

void Main()
{
	var trainingData = @"C:\Users\Misc\ImageTest\Training";
	var averagePath = @"C:\Users\Misc\ImageTest\average.bmp";
	var outputPath = @"C:\Users\Misc\ImageTest\test.gif";
	var datapath = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(datapath);

	var xSize = 120;
	var ySize = 120;
	var threshold = 400;

	CreateTrainingData(results, trainingData, xSize, ySize);
	var average = CreateAverage(trainingData);
	average.Save(averagePath, ImageFormat.Bmp);

	var encoder = new GifBitmapEncoder();
	for (int i = 0; i < results.Count; i++)
	{
		var dataset = results.Skip(i).Take(xSize).ToList();
		if (dataset.Count < xSize)
			break;
			
		var image = ReadNext(dataset, xSize, ySize);
		var value = EvaluateImage(average, image);
		if (value > threshold)
		{
			HighlightDetection(average, image);
		}
		AddFrame(encoder, image);
	}
	SaveAnimation(encoder, outputPath);
}

private void SaveAnimation(GifBitmapEncoder encoder, string outputPath)
{
	File.Delete(outputPath);
	using (FileStream fs = new FileStream(outputPath, FileMode.Create))
	{
		encoder.Save(fs);
	}
}

private void AddFrame(GifBitmapEncoder encoder, Bitmap image)
{
	if (image == null)
		return;
	
	var bmp = image.GetHbitmap();
	var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
		bmp,
		IntPtr.Zero,
		Int32Rect.Empty,
		BitmapSizeOptions.FromEmptyOptions());
	encoder.Frames.Add(BitmapFrame.Create(src));
	DeleteObject(bmp); // fixes a windows memory leak
}

private void HighlightDetection(Bitmap average, Bitmap testImage)
{
	var startX = testImage.Width - average.Width;
	var startY = testImage.Height - average.Height;
	for (int x = startX; x < testImage.Width; x++)
	{
		testImage.SetPixel(x, startX, Color.Red);
		testImage.SetPixel(x, testImage.Height - 1, Color.Red);
	}
	for (int y = startY; y < testImage.Height; y++)
	{
		testImage.SetPixel(startY, y, Color.Red);
		testImage.SetPixel(testImage.Width - 1, y, Color.Red);
	}
}

private void CreateTrainingData(List<Row> results, string trainingData, int xSize, int ySize)
{
	var count = 0;
	for (int i = xSize; i < results.Count - xSize; i += xSize)
	{
		var batch = results.GetRange(i, xSize);
		var minBatchIndex = batch.Select(x => x.Price).ToList().IndexOfMin();
		var minRangeIndex = (i - xSize) + minBatchIndex;
		var dataset = results.GetRange(minRangeIndex, xSize)
			.Select(x => new Row { Price = x.Price })
			.ToList();
		var image = ReadNext(dataset, xSize, ySize);
		image.Save($"{trainingData}\\{count++}.bmp", ImageFormat.Bmp);
	}
}

private Bitmap CreateAverage(string filepath)
{
	var images = Directory.GetFiles(filepath, "*.bmp")
		.Select(f => new Bitmap( Image.FromFile(f) )).ToList();
		
	if (!images.Any())
		return null;
	
	var xSize = images.Max(x => x.Width);
	var ySize = images.Max(x => x.Height);
	
	var average = new Bitmap(xSize, ySize);
	for (int x = 0; x < xSize; x++)
		for (int y = 0; y < ySize; y++)
		{
			var sum = images.Sum(i => i.GetPixel(x, y).R);
			var value = sum / images.Count();
			average.SetPixel(x, y, Color.FromArgb(value, value, value));
		}
	return average;
}

private int EvaluateImage(Bitmap average, Bitmap testImage)
{
	if (average == null || testImage == null)
		return 0;

	var sum = 0;
	
	var startX = testImage.Width - average.Width;
	var startY = testImage.Height - average.Height;
	for (int x = startX; x < testImage.Width; x++)
	{
		for (int y = startY; y < testImage.Height; y++)
		{
			if (testImage.GetPixel(x, y).R != 255)
				sum += 255 - average.GetPixel(x - startX, y - startY).R;
		}
	}
	return sum;
}

private Bitmap ReadNext(List<Row> dataset, int xSize, int ySize)
{
	if (!dataset.Any())
		return null;
	
	var yMax = dataset.Max(x => x.Price);
	var yMin = dataset.Min(x => x.Price);
	int yRange = Convert.ToInt32(yMax - yMin);
	if (yRange == 0)
		return null;

	var image = new Bitmap(xSize, ySize);
	for (int x = 0; x < dataset.Count; x++)
	{
		var price = (int)dataset[x].Price - yMin;
		var yPos = (int)((price / yRange) * ySize);

		for (int y = 0; y < ySize; y++)
		{
			if (y == yPos)
				image.SetPixel(x, y, Color.Black);
			else
				image.SetPixel(x, y, Color.White);
		}
	}
	return image;
}

public static class EnumerableEx
{
	public static IEnumerable<IEnumerable<T>> Batch<T>(
			this IEnumerable<T> source, int size)
	{
		T[] bucket = null;
		var count = 0;

		foreach (var item in source)
		{
			if (bucket == null)
				bucket = new T[size];

			bucket[count++] = item;

			if (count != size)
				continue;

			yield return bucket.Select(x => x);

			bucket = null;
			count = 0;
		}

		// Return the last bucket with all remaining elements
		if (bucket != null && count > 0)
			yield return bucket.Take(count);
	}

	public static int IndexOfMin(this IList<decimal> self)
	{
		if (self == null)
		{
			throw new ArgumentNullException("self");
		}

		if (self.Count == 0)
		{
			throw new ArgumentException("List is empty.", "self");
		}

		var min = self[0];
		int minIndex = 0;

		for (int i = 1; i < self.Count; ++i)
		{
			if (self[i] < min)
			{
				min = self[i];
				minIndex = i;
			}
		}

		return minIndex;
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

public class Row
{
	public DateTime Date {get; set;}
	public decimal Price {get; set;}
	public decimal Delta {get; set;}
}