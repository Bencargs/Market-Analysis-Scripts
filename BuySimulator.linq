<Query Kind="Program" />

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

	//var optimalParameter = MaximiseFunction(results, x => new Delta(x), 1, 200);

	var control = new Constant();
	var strategies = new Dictionary<Strategy, string>
	{
		{control, "Control"},
//		{new DoNothing(), "Do Nothing"},
//		{new DayOfWeek(System.DayOfWeek.Tuesday), "Day of Week"},
//		{new DayOfMonth(25), "Day of Month"},
//		{new FallValue(0.02m), "Falling Value"},
//		{new WaitPeriod(135), "Wait Period"},
		{new LinearRegression(149), "Linear Regression"},
		{new Delta(1), "Volatility"}
	};

	Simulate(results, strategies.Keys);

	DisplaySummary(strategies, control);

	DisplayCharts(strategies, control);
}

private void Simulate(List<Row> dataset, IEnumerable<Strategy> testStrategies, int startIndex = 0)
{
	foreach (var data in dataset.Skip(0))
	{
		foreach (var stratergy in testStrategies)
		{
			stratergy.Evaluate(data);
		}
	}
}

private void DisplayCharts(Dictionary<Strategy, string> testStrategies, Strategy control)
{
	var xSeries = Enumerable.Range(1, control.History.Count);

	//Totals Chart
	var totalsChart = xSeries.Chart();
	foreach (var stratergy in testStrategies)
	{
		totalsChart.AddYSeries(stratergy.Key.History, LINQPad.Util.SeriesType.Line, name: stratergy.Value);
	}
	totalsChart.Dump("Daily Price");


	//Delta Chart
	var deltasChart = xSeries.Chart();
	foreach (var stratergy in testStrategies)
	{
		var deltas = xSeries.Select(x => stratergy.Key.History[x - 1] - control.History[x - 1]);
		deltasChart.AddYSeries(deltas, LINQPad.Util.SeriesType.Line, name: stratergy.Value);
	}
	deltasChart.Dump("Daily Delta");
}

private void DisplaySummary(Dictionary<Strategy, string> testStrategies, Strategy control)
{
	// Totals
	foreach (var stratergy in testStrategies)
	{
		var title = stratergy.Value;
		var worth = stratergy.Key.GetWorth();
		worth.Dump($"{title} Total");
	}
	
	// Deltas
	foreach (var stratergy in testStrategies)
	{
		var title = stratergy.Value;
		var worth = stratergy.Key.GetWorth();
		var profit = (worth - control.GetWorth());
		$"{profit:C}".Dump($"{title} Profit vs Control ({profit / control.GetWorth():P})");
	}
	
	// Buy Histories
	foreach (var stratergy in testStrategies)
	{
		var title = stratergy.Value;
		stratergy.Key.BuyHistory.Dump($"{title} Buy History");
	}
}

private int MaximiseFunction(List<Row> dataset, Func<int, Strategy> function, int min, int max)
{
	return Enumerable.Range(1, 200).Select(x =>
	{
		var test = function(x);
		foreach (var data in dataset)
		{
			test.Evaluate(data);
		}
		return new { x, worth = test.GetWorth()};
	}).OrderByDescending(x => x.worth).First().x;
}

public class Delta : Strategy
{
	private int _delta;
	public Delta(int delta)
	{
		_delta = delta;
	}
	
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		return Math.Abs(data.Delta) < _delta;
	}
}

public class LinearRegression : Strategy
{
	protected List<XYPoint> _points = new List<UserQuery.LinearRegression.XYPoint>();
	protected int _window;
	
	public LinearRegression(int window)
	{
		_window = window;
	}
	
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		_points.Add(new XYPoint { X = _points.Count, Y = data.Price});
		var latestPoints = _points.AsEnumerable().Reverse().Take(_window).Reverse().ToList();
		if (latestPoints.Count < 2)
			return false;

		var lineOfBestFit = GenerateLinearBestFit(latestPoints);
		return (data.Price > lineOfBestFit.Last().Y);
	}

	protected static List<XYPoint> GenerateLinearBestFit(List<XYPoint> points)
	{
		int numPoints = points.Count;
		double meanX = points.Average(point => point.X);
		decimal meanY = points.Average(point => point.Y);

		double sumXSquared = Math.Pow(points.Sum(x => x.X), 2);
		decimal sumXY = points.Sum(point => point.X * point.Y);
		
		var meanSqrd = Math.Pow(meanX, 2);
		var back = (sumXSquared / numPoints - meanSqrd);
		var mid = numPoints - meanX * ((double) meanY);
		var front = ((double) sumXY);
		
		var a = (front / mid) / back;
		var b = (a * meanX - ((double)meanY));

		return points.Select(point => new XYPoint { X = point.X, Y = (decimal)(a * point.X - b) }).ToList();
	}

	protected class XYPoint
	{
		public int X;
		public decimal Y;
	}
}

public class FallValue : Strategy
{
	private decimal _lastPrice;
	private decimal _fallPercent;
	
	public FallValue(decimal fallPercent)
	{
		_fallPercent = fallPercent;
	}
	
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		var testValue = _lastPrice - ((decimal)_lastPrice * _fallPercent);
		_lastPrice = LatestPrice;
		if (data.Price < testValue)
		{
			return true;
		}
		return false;
	}
}

public class WaitPeriod : Strategy
{
	private int _daysToWait;
	private int _daysWaited;
	
	public WaitPeriod(int waitDays)
	{
		_daysToWait = waitDays;
		_daysWaited = 0;
	}

	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		_daysWaited++;
		if (_daysWaited == _daysToWait)
		{
			_daysWaited = 0;
			return true;
		}
		return false;
	}
}

public class DayOfWeek : Strategy
{
	private System.DayOfWeek _dayOfWeek;

	public DayOfWeek(System.DayOfWeek dayOfWeek)
	{
		_dayOfWeek = dayOfWeek;
	}

	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		return (data.Date.DayOfWeek == _dayOfWeek);
	}
}

public class DayOfMonth : Strategy
{
	private int _dayOfMonth;
	
	public DayOfMonth(int dayOfMonth)
	{
		_dayOfMonth = dayOfMonth;
	}
	
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		return data.Date.Day == _dayOfMonth;
	}
}

public class Constant : Strategy
{
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		return true;
	}
}

public class DoNothing : Strategy
{
	protected override bool ShouldAddFunds(Row data)
	{
		return true;
	}

	protected override bool ShouldBuyShares(Row data)
	{
		return false;
	}
}


public abstract class Strategy
{
	protected Strategy()
	{
		Shares = 0;
		Funds = 0m;
		History = new List<decimal>();
		BuyHistory = new List<Row>();
	}
	
	public virtual void Evaluate(Row data)
	{
		UpdateState(data);
		
		if (ShouldAddFunds(data))
			AddFunds();
			
		if (ShouldBuyShares(data))
			BuyShares();
	}
	
	public virtual decimal GetWorth()
	{
		return Funds + (Shares * LatestPrice);
	}
	
	protected decimal Shares {get; set;}
	protected decimal Funds {get; set;}
	protected decimal LatestPrice { get; set; }
	protected DateTime LatestDate {get; set;}
	public List<decimal> History { get; set;}
	public List<Row> BuyHistory {get; set;}
	
	protected abstract bool ShouldAddFunds(Row data);
	protected abstract bool ShouldBuyShares(Row data);
	
	protected virtual void UpdateState(Row data)
	{
		LatestDate = data.Date;
		LatestPrice = data.Price;
		History.Add(GetWorth());
	}
	
	protected virtual void AddFunds()
	{
		Funds += 10;		
	}
	
	protected virtual void BuyShares()
	{
		var newShares = Funds / LatestPrice;
		Shares += newShares;
		Funds = 0;
		BuyHistory.Add(new Row{Date = LatestDate, Price = LatestPrice});
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
