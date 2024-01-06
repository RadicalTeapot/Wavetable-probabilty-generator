
GenerateData();

double Identity(double x) => x;
double NormalizedSigmoid(double x, double k, double offset)
{
    x = x * 2 - 1; // Remap to -1, 1
    x = (x - offset) / (1 + offset); // Offset and scale
    k = Math.Min(Math.Max(k, -1), 1); // Clamp to range
    double y = x * (1 - k) / (k * (1 - 2 * Math.Abs(x)) + 1);
    y = y * 0.5 + 0.5; // Remap to 0, 1
    return Math.Min(Math.Max(y, 0), 1); // Clamp to range
}

void GenerateData() {
    const string basePath = "C:/tmp/probability_table_generator/out";
    const string baseFileName = "table_";
    const string baseFileExt = ".wav";

    const int tableSize = 256;
    const int tableCount = 64;

    const int sampleByteSize = 2;
    const int numChannels = 1;
    const int sampleRate = 44100;

    CurveValue[] majorKey = new[] {
        new CurveValue {Start = -1.0/7.0, End = 0.0/7.0, Map = Identity}, // I
        new CurveValue {Start = 5.0/7.0, End = 6.0/7.0, Map = Identity},  // II
        new CurveValue {Start = 2.0/7.0, End = 3.0/7.0, Map = Identity},  // III
        new CurveValue {Start = 3.0/7.0, End = 4.0/7.0, Map = Identity},  // IV
        new CurveValue {Start = 1.0/7.0, End = 2.0/7.0, Map = Identity},  // V
        new CurveValue {Start = 4.0/7.0, End = 5.0/7.0, Map = Identity},  // VI
        new CurveValue {Start = 6.0/7.0, End = 7.0/7.0, Map = Identity}   // VII
    };

    CurveValue[] minorPentatonic = new[] {
        new CurveValue {Start = -1.0/7.0, End = 0.0/7.0, Map = Identity}, // I
        new CurveValue {Start = 2.0/7.0, End = 5.0/7.0, Map = Identity},  // II
        new CurveValue {Start = 1.0/7.0, End = 3.0/7.0, Map = Identity},  // III
        new CurveValue {Start = 3.0/7.0, End = 7.0/7.0, Map = Identity},  // IV
        new CurveValue {Start = 4.0/7.0, End = 7.0/7.0, Map = Identity},  // V
    };

    for (int i = 0; i < tableCount; i++) {
        double normalizedIndex = (double)i / tableCount;
        short[] data = GenerateTableAtIndex(normalizedIndex, tableSize, minorPentatonic);

        WaveFileData waveFileData = new()
        {
            SampleByteSize = sampleByteSize,
            NumChannels = numChannels,
            SampleRate = sampleRate,
            Data = data
        };

        string filePath = Path.Join(basePath, $"{baseFileName}{i:D4}{baseFileExt}");
        // TODO Make sure directory exists and is empty before calling the write to wave method
        WriteDataToWavFile(filePath, waveFileData);
    }
}

short[] GenerateTableAtIndex(double normalizedIndex, int tableSize, CurveValue[] key)
{
    double[] proportions = new double[key.Length];
    double sumProportions = 0;
    for (int i = 0; i < key.Length; i++) {
        double x = (normalizedIndex - key[i].Start) / (key[i].End - key[i].Start);
        x = key[i].Map(x);
        x = Math.Min(Math.Max(x, 0), 1);
        proportions[i] = x;
        sumProportions += x;
    }

    int[] indexRange = new int[key.Length];
    int usedRange = 0;
    for (int i = key.Length-1; i > 0; i--) {
        int range = (int)Math.Floor(tableSize * proportions[i] / sumProportions);
        usedRange += range;
        indexRange[i] = range;
    }
    indexRange[0] = Math.Max(tableSize - usedRange, 0);

    int lastIndex = 0;
    short[] table = new short[tableSize];
    for (int i = 0; i < key.Length; i++) {
        Array.Fill(table, (short)(ushort.MaxValue * i/(key.Length-1) - short.MinValue), lastIndex, indexRange[i]);
        lastIndex += indexRange[i];
    }

    return table;
}

void WriteDataToWavFile(string filePath, WaveFileData data)
{
    using var stream = File.Open(filePath, FileMode.Create);
    using var wr = new BinaryWriter(stream, System.Text.Encoding.ASCII, false);

    wr.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    wr.Write((uint)(44 + data.Data.Length * data.SampleByteSize));
    wr.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    wr.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    wr.Write((uint)16); // Length format data above in bytes
    wr.Write((ushort)1); // Encoding (1 for PCM)
    wr.Write((ushort)data.NumChannels); // Channels
    wr.Write((uint)data.SampleRate); // Sample rate
    wr.Write((uint)(data.SampleRate * data.SampleByteSize * data.NumChannels)); // Average bytes per second
    wr.Write((ushort)(data.SampleByteSize * data.NumChannels)); // Type of audio (2 = 16 bit mono)
    wr.Write((ushort)(8 * data.SampleByteSize)); // bits per sample
    wr.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    wr.Write((uint)(data.Data.Length * data.SampleByteSize));
    for (int i = 0; i < data.Data.Length; i++)
        wr.Write(data.Data[i]);
}

struct CurveValue
{
    public double Start;
    public double End;
    public Func<double, double> Map;
};

struct WaveFileData
{
    public int SampleByteSize;
    public int NumChannels;
    public int SampleRate;
    public short[] Data;
}
