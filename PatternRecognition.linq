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
</Query>

[System.Runtime.InteropServices.DllImport("gdi32.dll")]
public static extern bool DeleteObject(IntPtr hObject);

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

	var gifEncoder = new GifBitmapEncoder();

	var batch = 0;
	var xSize = 30;
	var ySize = 30;
	for (int i = 30; i < results.Count; i++)
	{
		var set = results.Skip(i).Take(xSize).ToList();
		if (set.Count < xSize)
			break; // todo: fix this tomorrow
			
		// convert each batch into image
		var yMax = set.Max(x => x.Price);
		var yMin = set.Min(x => x.Price);
		int yRange = Convert.ToInt32(yMax - yMin);

		var image = new Bitmap(xSize, ySize);
		Graphics flagGraphics = Graphics.FromImage(image);

		for (int x = 0; x < xSize; x++)
		{
			var price = (int)set[x].Price - yMin;
			var yPos = (int)((price / yRange) * ySize);

			for (int y = 0; y < ySize; y++)
			{
				if (y == yPos)
					image.SetPixel(x, y, Color.Black);
				else
					image.SetPixel(x, y, Color.White);
			}
		}

		var bmp = image.GetHbitmap();
		var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
			bmp,
			IntPtr.Zero,
			Int32Rect.Empty,
			BitmapSizeOptions.FromEmptyOptions());
		gifEncoder.Frames.Add(BitmapFrame.Create(src));
		DeleteObject(bmp); // fixes a windows memory leak
	}
	using (FileStream fs = new FileStream(@"C:\Users\Ben\Downloads\ImageTest\test.gif", FileMode.Create))
	{
		gifEncoder.Save(fs);
	}
}

//void Main()
//{
//	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
//	var results = ReadOpeningPrices(path);
//
//	var b = 0;
//	var xSize = 30;
//	var ySize = 30;
//	foreach (var set in results.Batch(xSize))
//	{
//		// convert each batch into image
//		var yMax = set.Max(x => x.Price);
//		var yMin = set.Min(x => x.Price);
//		int yRange = Convert.ToInt32( yMax - yMin );
//		
//		var image = new Bitmap(xSize, ySize);
//		Graphics flagGraphics = Graphics.FromImage(image);
//		
//		var list = set.ToList();
//		if (list.Count < xSize)
//			return;// Yeet out coz I CBF
//			
//		for (int x = 0; x < xSize; x++)
//		{
//			var price = (int) list[x].Price - yMin;
//			var yPos = (int) (( price / yRange ) * ySize);
//			
//			for (int y = 0; y < ySize; y++)
//			{
//				if (y == yPos)
//					image.SetPixel(x, y, Color.Black);
//				else
//					image.SetPixel(x, y, Color.White);
//			}
//		}
//		
//		image.Save($@"C:\Users\Ben\Downloads\ImageTest\{++b}.bmp");
//	}
//	return;
//}

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

// Define other methods and classes here
