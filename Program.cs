using System.Text.Json;

try
{
    if (args.Length != 1) throw new ArgumentException("Pass one JSON input file");
    using var data = JsonDocument.Parse(File.ReadAllText(args[0]));
    Console.WriteLine(JsonSerializer.Serialize(Analyze(data.RootElement)));
    return 0;
}
catch (Exception error) when (error is IOException or JsonException or FormatException
                               or ArgumentException or InvalidOperationException
                               or KeyNotFoundException or OverflowException)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}

static object Analyze(JsonElement input)
{
    var capacity = input.GetProperty("capacity").GetDecimal();
    var current = input.GetProperty("initial").GetDecimal();
    var target = input.GetProperty("target").GetDecimal();
    if (capacity <= 0 || current < 0 || current > capacity || target < 0 || target > capacity)
        throw new ArgumentException("Invalid capacity, charge, or target");
    decimal hours = 0;
    decimal? targetHour = current >= target ? 0m : null;
    var trace = new List<object>();
    foreach (var segment in input.GetProperty("segments").EnumerateArray())
    {
        if (segment.GetArrayLength() != 3) throw new ArgumentException("Expected hours, charge and load");
        var duration = segment[0].GetDecimal();
        var charge = segment[1].GetDecimal();
        var load = segment[2].GetDecimal();
        if (duration <= 0 || charge < 0 || load < 0) throw new ArgumentException("Invalid segment");
        var rate = charge-load;
        var end = Math.Clamp(current+duration*rate,0,capacity);
        if (targetHour is null && rate > 0 && end >= target) targetHour = hours+(target-current)/rate;
        current = end;
        hours += duration;
        trace.Add(new { hour = hours, energy = current });
    }
    return new { final = current, targetHour, trace };
}
