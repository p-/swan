﻿namespace Swan.Samples;

using Collections;
using Formatters;
using Logging;
using Mapping;
using Microsoft.Data.Sqlite;
using Platform;
using Reflection;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Threading;

public static partial class Program
{
    private static void CsvSketchpad()
    {
        var n = new SqliteParameter();

        var csvContent = "name, value, ts, DT\r\nMy name is foo,2,00:23:40,2011-02-01\r\n5,6,7,8\r\n";
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        using var csvReader = new CsvObjectReader<MockInfo>(csvStream, Encoding.UTF8);
        foreach (var dict in csvReader)
        {
            var hello = dict.Name;
        }

        var ti = typeof(int).TypeInfo();

        var myList = new List<int>();
        if (CollectionProxy.TryCreate(myList, out var proxy))
        {
            var p = proxy.Add("1");
            p = proxy.Add(2);
            p = proxy.Add(34599.44d);

            var strings = new string[6];
            proxy.CopyTo(strings, 0);

            proxy.Remove("2");
            proxy.RemoveAt(0);
            _ = proxy.SyncRoot;
            _ = proxy.IsSynchronized;
            _ = proxy.IsFixedSize;
            _ = proxy.IsSynchronized;

            proxy.Insert(0, "3");
            // proxy.Add(1, 2);

            var it = proxy["1"];
            proxy.Clear();
        }
    }

    private static void ChangeTypeSketchpad()
    {
        var source = new int?();
        var e1 = FirstEnum.Two;
        var e2 = SecondEnum.Eleven;
        if (TypeManager.TryChangeType(source, typeof(double?), out var value))
        {

        }

        if (TypeManager.TryChangeType(e1, typeof(SecondEnum?), out var v2))
        {

        }

        if (TypeManager.TryChangeType("2", typeof(FirstEnum?), out var v1))
        {

        }

        if (TypeManager.TryChangeType("three", typeof(FirstEnum?), out var v3))
        {

        }
    }

    private static void Sketchpad()
    {
        var x = new ExpandoObject();
        (x as IDictionary<string, object?>).Add("Hello World", 2);

        var mockContents = $"\r\n,,,\r\nName, Value, TS, Date, Id\r\nHello, 54556, 00:30:15, {Guid.NewGuid()}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mockContents));
        using var reader = new CsvDictionaryReader(stream, Encoding.UTF8)
            .Skip(2)
            .AddMapping("TS", "timespan")
            .RemoveMapping("Value")
            .AddMapping("Date", "Transformed Date", (s) => $"New date is: {s}");


        var readerEntries = reader.ToList();

        dynamic objectOne = new ExpandoObject();
        dynamic objectTwo = new ExpandoObject();

        objectOne.Name = "Parent Object";
        objectOne.Age = 38;
        objectOne.Child = objectTwo;
        objectOne.IntArray = new[] { 1, 2, 3, 4, 5 };

        objectTwo.Name = "Child Object";
        objectTwo.Age = 2;
        objectTwo.Parent = objectOne;
        objectTwo.Mock = new MockInfo();

        dynamic container = new ExpandoObject();
        container.One = objectOne;
        container.Two = objectTwo;

        var circularJson = TextSerializer.Serialize(container, TextSerializerOptions.HumanReadable);

        // return;
        var boolValue = true;
        var atomic = new AtomicBoolean();

        var simpleInt = 0;
        var atomic2 = new AtomicInteger(-1024);
        simpleInt = atomic2;
        var isZero = atomic2 >= simpleInt;
        atomic2.Increment();

        var d = new Dictionary<int, string> { [1] = "Hello\t\r\n\0\t\r\n", [2] = "World" };

        var arrayOfArrays = new double[2][];
        arrayOfArrays[0] = new[] { 1d, 2d, 3d, 4d };
        arrayOfArrays[1] = new[] { 5d, 6d, 7d, 8d };

        var twoDimensionalArray = new double[2, 2];
        twoDimensionalArray[0, 0] = 1d;
        twoDimensionalArray[0, 1] = 2d;
        twoDimensionalArray[1, 0] = 3d;
        twoDimensionalArray[1, 1] = 4d;

        dynamic expando = new ExpandoObject();
        expando.Property1 = "one property";
        expando.Property2 = new[] { 6, 7, 8, 9, 10 };
        expando.Property3 = new List<MockInfo> { new(), new() };
        expando.Property4 = arrayOfArrays;
        expando.Property5 = twoDimensionalArray;

        var mock = new MockInfo();
        var sdict = new SubDict
        {
            ["Hello"] = 43,
            ["World"] = "Some text here",
            ["array"] = new[] { 6, 7, 8, 9, 10 },
            ["object"] = new Dictionary<string, string> { ["x"] = "Y" },
            ["dynamicObj"] = expando
        };

        var outText0 = TextSerializer.Serialize(sdict);

        var outDict = JsonSerializer.Deserialize<SubDict>(outText0);

        var outText = TextSerializer.Serialize(d);
        var outText2 = TextSerializer.Serialize(mock);

        var objDict = new Dictionary<object, object?>()
        {
            ["jsonstuff"] = outDict,
            [TimeSpan.FromMilliseconds(334344)] = sdict,
            [mock] = sdict,
            ["object"] = new MockInfo { Name = "OTHER" }
        };

        TextSerializer.Serialize(objDict, TextSerializerOptions.JsonPrettyPrint);

        var optionsJson = TextSerializer.Serialize(TextSerializerOptions.JsonCompactCamel);
        var _ = JsonSerializer.Deserialize<TextSerializerOptions>(optionsJson);
    }

    /// <summary>
    /// Entry point of the Program.
    /// </summary>
    public static async Task Main()
    {
        // Sketchpad();
        // Await DataPlayground.AsyncQuerying();
        //return;

        await DataPlayground.BasicExample();

        return;
        Logger.RegisterLogger<FileLogger>();

        TestJson();
        await TestTerminalOutputs();
        TestExceptionLogging();

        TestFastOutput();
        TestReadPrompt();
        TestCsvFormatters();
        Terminal.Flush();
        Terminal.ReadKey("Enter any key to exit . . .");
    }

    private static void TestExceptionLogging()
    {
        try
        {
            throw new SampleException();
        }
        catch (Exception ex)
        {
            ex.Log(typeof(Program), "Exception dump starts");
        }
    }

    private static void TestJson()
    {
        var instance = new SampleCopyTarget { AlternateId = 10, CreationDate = new(2010, 1, 1), Id = 1, Score = "A" };

        var payload = instance.JsonSerialize();

        payload.Info(typeof(Program));

        var recover = payload.JsonDeserialize<SampleCopyTarget>();

        recover.Dump(typeof(Program));

        var jsonText =
            "{\"SimpleProperty\": \"SimpleValue\", \"EmptyProperty\": \"\\/Forward-Slash\\\"\", \"EmptyArray\": [], \"EmptyObject\": {}}";
        var jsonObject = JsonSerializer.Deserialize<object>(jsonText);
        jsonObject.Dump(typeof(Program));

        jsonText =
            "{\"SimpleProperty\": \r\n     \"SimpleValue\", \"EmptyProperty\": \" \", \"EmptyArray\": [  \r\n \r\n  ], \"EmptyObject\": { } \r\n, \"NumberStringArray\": [1,2,\"hello\",4,\"666\",{ \"NestedObject\":true }] }";
        jsonObject = jsonText.JsonDeserialize();
        jsonObject.Dump(typeof(Program));

        "test".Dump(typeof(Program));
    }

    private static void TestFastOutput()
    {
        var limit = Console.BufferHeight;
        for (var i = 0; i < limit; i += 25)
        {
            Terminal.WriteLine($"Output info {i} ({((decimal)i / limit):P})");
            Terminal.BacklineCursor();
        }
    }

    private static void TestReadPrompt()
    {
        // Terminal.Clear();
        var sampleOptions = new Dictionary<ConsoleKey, string>
        {
            {ConsoleKey.A, "Sample A"},
            {ConsoleKey.B, "Sample B"},
            {ConsoleKey.C, "Sample C"},
            {ConsoleKey.D, "Sample D"},
            {ConsoleKey.E, "Sample E"}
        };

        Terminal.ReadPrompt("Please provide an option", sampleOptions);
    }

    private static async Task TestTerminalOutputs()
    {
        for (var i = 0; i <= 100; i++)
        {
            await Task.Delay(20);
            Terminal.OverwriteLine($"Current Progress: {(i + "%"),-10}");
        }

        if (Terminal.ReadKey("Press a key to test logging output. (X) will exit.").Key == ConsoleKey.X) return;
        Terminal.WriteLine("OUTPUT LOGGING TEST", ConsoleColor.Blue);
        "This is some error".Error(typeof(Program));
        "This is some error".Error(nameof(TestTerminalOutputs));
        "This is some info".Info(typeof(Program));
        "This is some info".Info(nameof(TestTerminalOutputs));
        "This is some warning".Warn(typeof(Program));
        "This is some warning".Warn(nameof(TestTerminalOutputs));
        "This is some tracing info".Trace(typeof(Program));
        "This is some tracing info".Trace(nameof(TestTerminalOutputs));
        "This is for debugging stuff".Debug(typeof(Program));
        "This is for debugging stuff".Debug(nameof(TestTerminalOutputs));

        // The simplest way of writing a line of text:
        Terminal.WriteLine($"Hello, today is {DateTime.Today}");

        // Now, add some color:
        Terminal.WriteLine($"Hello, today is {DateTime.Today}", ConsoleColor.Green);

        if (Terminal.ReadKey("Press a key to test menu options. (X) will exit.").Key == ConsoleKey.X) return;
        Terminal.WriteLine("TESTING MENU OPTIONS", ConsoleColor.Blue);

        var sampleOptions = new Dictionary<ConsoleKey, string> { { ConsoleKey.A, "Sample A" }, { ConsoleKey.B, "Sample B" } };

        Terminal.ReadPrompt("Please provide an option", sampleOptions, "Exit this program");
    }

    private static void TestCsvFormatters()
    {
        var test01FilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var test02FilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var generatedRecords = SampleCsvRecord.CreateSampleSet(100);
        $"Generated {generatedRecords.Count} sample records.".Info(nameof(TestCsvFormatters));

        var savedRecordCount = Csv.Save(generatedRecords, test01FilePath);
        $"Saved {savedRecordCount} records (including header) to file: {Path.GetFileName(test01FilePath)}."
            .Info(nameof(TestCsvFormatters));

        var loadedRecords = Csv.Load<SampleCsvRecord>(test01FilePath);
        $"Loaded {loadedRecords.Count} records from file: {Path.GetFileName(test01FilePath)}.".Info(
            nameof(TestCsvFormatters));

        savedRecordCount = Csv.Save(generatedRecords, test02FilePath);
        $"Saved {savedRecordCount} records (including header) to file: {Path.GetFileName(test02FilePath)}."
            .Info(nameof(TestCsvFormatters));

        var sourceObject = loadedRecords[generatedRecords.Count / 2];
        var targetObject = new SampleCopyTarget();
        var copiedProperties = sourceObject.CopyPropertiesTo(targetObject);
        $"{nameof(MappingExtensions.CopyPropertiesTo)} method copied {copiedProperties} properties from one object to another"
            .Info(nameof(TestCsvFormatters));
    }
}
