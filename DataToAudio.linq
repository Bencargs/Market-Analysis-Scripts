<Query Kind="Program" />

void Main()
{
	var path = @"C:\Users\Ben\Documents\Shares\au.investing\S&P_ASX 300 Historical Data.csv";
	var results = ReadOpeningPrices(path);
	
	var dataArray = GetDeltaByMonth(results);
	
	var xAxis = 0;
	dataArray.Chart(c => xAxis++, v => v).Dump();
	
	var header = new WaveHeader();
	var format = new WaveFormatChunk();
	var data = new WaveDataChunk();
	
	uint seconds = 30;
	uint numSamples = format.dwSamplesPerSec * format.wChannels * seconds;
	data.shortArray = new short[numSamples];

	int amplitude = 16380;  // 32760 is Max amplitude for 16-bit audio
  	//double freq = 440.0f;   // Concert A: 440Hz
	//double baseFrequency = (Math.PI * 2 * freq) / (format.dwSamplesPerSec * format.wChannels);
	
	// as the data array is smaller than the sound array, determine how long to hold each data point
	var divider = numSamples / dataArray.Count() + 1;	
	
	for (uint i = 0; i < numSamples - 1; i++)
    {
		var dataIndex = (int) ((double)i / divider);
		var baseFrequency = (float) (dataArray[dataIndex]) / 10000;	// divide by 1000 just to quiet it down a bit
		
        // Fill with a simple sine wave at max amplitude
        for (int channel = 0; channel < format.wChannels; channel++)
        {
			var frequency = amplitude * Math.Sin(baseFrequency * i);
        	data.shortArray[i + channel] = Convert.ToInt16(frequency);
        }
    }
	
	data.dwChunkSize = (uint)(data.shortArray.Length * (format.wBitsPerSample / 8));
	
	var filepath = @"C:\Users\Ben\Documents\Shares\au.investing\test.wav";
	File.Delete(filepath);
	Save(@"C:\Users\Ben\Documents\Shares\au.investing\test.wav", header, format, data);
}

private List<short> GetDeltaByMonth(List<Row> dailyData)
{
	var results = dailyData
		.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1))
		.ToDictionary(k => k.Key, v => v.ToList())
		.Select(x => new {Date = x.Key, Price = x.Value.Sum(y => y.Price) / x.Value.Count()})
		.ToArray();
	
	var dataArray = new List<short>();
	for (int i = 1; i < results.Length; i++)
	{
		var delta = Convert.ToInt16(results[i - 1].Price - results[i].Price);
		dataArray.Add(delta);
	}
	return dataArray;
}

public void Save(string filePath, WaveHeader header, WaveFormatChunk format, WaveDataChunk data)
{
    // Create a file (it always overwrites)
    FileStream fileStream = new FileStream(filePath, FileMode.Create);   
 
    // Use BinaryWriter to write the bytes to the file
    BinaryWriter writer = new BinaryWriter(fileStream);
 
    // Write the header
    writer.Write(header.sGroupID.ToCharArray());
    writer.Write(header.dwFileLength);
    writer.Write(header.sRiffType.ToCharArray());
 
    // Write the format chunk
    writer.Write(format.sChunkID.ToCharArray());
    writer.Write(format.dwChunkSize);
    writer.Write(format.wFormatTag);
    writer.Write(format.wChannels);
    writer.Write(format.dwSamplesPerSec);
    writer.Write(format.dwAvgBytesPerSec);
    writer.Write(format.wBlockAlign);
    writer.Write(format.wBitsPerSample);
 
    // Write the data chunk
    writer.Write(data.sChunkID.ToCharArray());
    writer.Write(data.dwChunkSize);
    foreach (short dataPoint in data.shortArray)
    {
        writer.Write(dataPoint);
    }
 
    writer.Seek(4, SeekOrigin.Begin);
    uint filesize = (uint)writer.BaseStream.Length;
    writer.Write(filesize - 8);
    
    // Clean up
    writer.Close();
    fileStream.Close();            
}

public class WaveHeader
{
    public string sGroupID; // RIFF
    public uint dwFileLength; // total file length minus 8, which is taken up by RIFF
    public string sRiffType; // always WAVE
 
    /// <summary>
    /// Initializes a WaveHeader object with the default values.
    /// </summary>
    public WaveHeader()
    {
        dwFileLength = 0;
        sGroupID = "RIFF";
        sRiffType = "WAVE";
    }
}

public class WaveFormatChunk
{
    public string sChunkID;         // Four bytes: "fmt "
    public uint dwChunkSize;        // Length of header in bytes
    public ushort wFormatTag;       // 1 (MS PCM)
    public ushort wChannels;        // Number of channels
    public uint dwSamplesPerSec;    // Frequency of the audio in Hz... 44100
    public uint dwAvgBytesPerSec;   // for estimating RAM allocation
    public ushort wBlockAlign;      // sample frame size, in bytes
    public ushort wBitsPerSample;    // bits per sample
 
    /// <summary>
    /// Initializes a format chunk with the following properties:
    /// Sample rate: 44100 Hz
    /// Channels: Stereo
    /// Bit depth: 16-bit
    /// </summary>
    public WaveFormatChunk()
    {
        sChunkID = "fmt ";
        dwChunkSize = 16;
        wFormatTag = 1;
        wChannels = 2;
        dwSamplesPerSec = 44100;
        wBitsPerSample = 16;
        wBlockAlign = (ushort)(wChannels * (wBitsPerSample / 8));
        dwAvgBytesPerSec = dwSamplesPerSec * wBlockAlign;            
    }
}

public class WaveDataChunk
{
    public string sChunkID;     // "data"
    public uint dwChunkSize;    // Length of header in bytes
    public short[] shortArray;  // 8-bit audio
 
    /// <summary>
    /// Initializes a new data chunk with default values.
    /// </summary>
    public WaveDataChunk()
    {
        shortArray = new short[0];
        dwChunkSize = 0;
        sChunkID = "data";
    }   
}

public static short ConvertRange(
    decimal originalStart, decimal originalEnd, // original range
    decimal value) // value to convert
{
	var newStart = short.MinValue;
	var newEnd = short.MaxValue;
    var scale = (decimal)(newEnd - newStart) / (originalEnd - originalStart);
    var intVal = (int)(newStart + ((value - originalStart) * scale));
	return Convert.ToInt16(intVal);
}

public List<Row> ReadOpeningPrices(string path)
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
	public int Volatility {get; set;}
}