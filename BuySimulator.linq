<Query Kind="Program" />

void Main()
{
	var path = @"C:\Source\Misc\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);

	// Stratergy Maximiser
//	Enumerable.Range(1, 200).Select(x =>
//	{
//		var test = new Delta(x);
//		foreach (var data in results)
//		{
//			test.Evaluate(data);
//		}
//		return new { x, worth = test.GetWorth()};
//	}).OrderByDescending(x => x.worth).First().Dump();
//	return;

	var control = new Constant();
	var doNothing = new DoNothing();
	var dayOfWeek = new DayOfWeek(System.DayOfWeek.Tuesday);
	var dayOfMonth = new DayOfMonth(25);
	var fallValue = new FallValue(0.02m);
	var waitPeriod = new WaitPeriod(135);
	var linearRegression = new LinearRegression(149);
	var delta = new Delta(1);
	foreach (var data in results)
	{
		control.Evaluate(data);
		doNothing.Evaluate(data);
		dayOfMonth.Evaluate(data);
		dayOfWeek.Evaluate(data);
		fallValue.Evaluate(data);
		linearRegression.Evaluate(data);
		waitPeriod.Evaluate(data);
		delta.Evaluate(data);
	}
	
	// Totals
	control.GetWorth().Dump("Control Total");
//	dayOfMonth.GetWorth().Dump();
	linearRegression.GetWorth().Dump("Linear Regression Total");
	delta.GetWorth().Dump("Volatility Total");

	// Deltas
	var linearProfit = (linearRegression.GetWorth() - control.GetWorth());
	$"{linearProfit:C}".Dump($"Linear Regression Profit vs Control ({linearProfit / control.GetWorth():P})");
	var deltaProfit = (delta.GetWorth() - control.GetWorth());
	$"{deltaProfit:C}".Dump($"Volatility Profit vs Control ({deltaProfit / control.GetWorth():P})");


	//Total Chart
	var xSeries = Enumerable.Range(1, control.History.Count);
	xSeries.Chart()
		.AddYSeries(control.History, LINQPad.Util.SeriesType.Line, name: "Control")
//		.AddYSeries(doNothing.History, LINQPad.Util.SeriesType.Line, name: "Do Nothing")
//		.AddYSeries(dayOfMonth.History, LINQPad.Util.SeriesType.Line, "Day of Month")
//		.AddYSeries(dayOfWeek.History, LINQPad.Util.SeriesType.Line, "Day of Week")
//		.AddYSeries(waitPeriod.History, LINQPad.Util.SeriesType.Line, "Wait Period")
//		.AddYSeries(fallValue.History, LINQPad.Util.SeriesType.Line, name: "Fall Value")
		.AddYSeries(linearRegression.History, LINQPad.Util.SeriesType.Line, name: $"Linear Regression")
		.AddYSeries(delta.History, LINQPad.Util.SeriesType.Line, name: $"Volatility Stratergy")
		.Dump("Daily Price");


	//Delta Chart
	var dailyDelta = new List<Decimal>();
	var linearDelta = new List<Decimal>();
	var nothingDelta = new List<Decimal>();
	foreach (var day in xSeries)
	{
		dailyDelta.Add(delta.History[day - 1] - control.History[day - 1]);
		linearDelta.Add(linearRegression.History[day - 1] - control.History[day - 1]);
		nothingDelta.Add(doNothing.History[day - 1] - control.History[day - 1]);
	}
	xSeries.Chart()
		.AddYSeries(dailyDelta, LINQPad.Util.SeriesType.Line, "Volatility Stratergy")
		.AddYSeries(linearDelta, LINQPad.Util.SeriesType.Line, name: "Linear Regression")
		//		.AddYSeries(nothingDelta, LINQPad.Util.SeriesType.Line, name: "Do Nothing Stratergy")
		.Dump("Daily Delta");
}

public class Delta : Stratergy
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

public class LinearRegression : Stratergy
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

public class FallValue : Stratergy
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

public class WaitPeriod : Stratergy
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

public class DayOfWeek : Stratergy
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

public class DayOfMonth : Stratergy
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

public class Constant : Stratergy
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

public class DoNothing : Stratergy
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


public abstract class Stratergy
{
	protected Stratergy()
	{
		Shares = 0;
		Funds = 0m;
		History = new List<decimal>();
	}
	
	public virtual void Evaluate(Row data)
	{
		LatestPrice = data.Price;
		History.Add(GetWorth());
		
		if (ShouldAddFunds(data))
			AddFunds();
			
		if (ShouldBuyShares(data))
			BuyShares();
	}
	
	public virtual decimal GetWorth()
	{
		return Funds + (Shares * LatestPrice);
	}
	
	protected virtual decimal Shares {get; set;}
	protected virtual decimal Funds {get; set;}
	protected virtual decimal LatestPrice { get; set; }
	public virtual List<decimal> History { get; set;}
	
	protected abstract bool ShouldAddFunds(Row data);
	protected abstract bool ShouldBuyShares(Row data);
	
	protected virtual void AddFunds()
	{
		Funds += 10;		
	}
	
	protected virtual void BuyShares()
	{
		var newShares = Funds / LatestPrice;
		Shares += newShares;
		Funds = 0;
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
