﻿namespace Swan.Formatters;

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Threading;

/// <summary>
/// Represents a base class for all CSV readers, which typically read
/// a comma-separated set of values that can be configured to read an parse tabular data with flexible
/// encoding, field separators and escape characters.
/// </summary>
public abstract class CsvReaderBase<TLine> : ICsvReader<TLine>
{
    private const int BufferSize = 4096;
    private readonly AtomicBoolean _isDisposed = new();
    private readonly AtomicInteger _count = new();
    private readonly StreamReader _reader;

    /// <summary>
    /// Creates a new instance of the <see cref="CsvReaderBase{TLine}"/> class.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <param name="separatorChar">The field separator character.</param>
    /// <param name="escapeChar">The escape character.</param>
    /// <param name="leaveOpen">true to leave the stream open after the stream reader object is disposed; otherwise, false.</param>
    /// <param name="trimsValues">True to trim field values as they are read and parsed.</param>
    protected CsvReaderBase(Stream stream,
        Encoding? encoding,
        char separatorChar,
        char escapeChar,
        bool leaveOpen,
        bool trimsValues)
    {
        var streamEncoding = encoding ?? Csv.DefaultEncoding;
        var detectBom = streamEncoding.GetPreamble().Length > 0;
        _reader = new(stream, streamEncoding, detectBom, BufferSize, leaveOpen);
        SeparatorChar = separatorChar;
        EscapeChar = escapeChar;
        TrimsValues = trimsValues;
    }

    /// <summary>
    /// Gets the escape character.
    /// </summary>
    public char EscapeChar { get; }

    /// <summary>
    /// Gets the separator character.
    /// </summary>
    public char SeparatorChar { get; }

    /// <summary>
    /// Gets a value indicating whether the reader trims or removes
    /// whitespace from the records it reads.
    /// </summary>
    public bool TrimsValues { get; }

    /// <inheritdoc />
    public bool EndOfStream => _reader.EndOfStream;

    /// <summary>
    /// Gets the encoding of the underlying <see cref="StreamReader"/>.
    /// </summary>
    public Encoding Encoding => _reader.CurrentEncoding;

    /// <inheridoc />
    public int Count => _count.Value;

    /// <inheridoc />
    public IReadOnlyList<string>? Values
    {
        get;
        private set;
    }

    /// <inheridoc />
    public bool IsDisposed
    {
        get => _isDisposed.Value;
        private set => _isDisposed.Value = value;
    }

    /// <inheridoc />
    public abstract TLine Current { get; }

    /// <inheridoc />
    object IEnumerator.Current =>
        Current ??
        throw new InvalidOperationException("The reader is not in a valid state.");

    /// <inheridoc />
    public void Skip(int skipCount = 1)
    {
        if (skipCount < 1)
            throw new ArgumentOutOfRangeException(nameof(skipCount));

        for (var i = 0; i < skipCount; i++)
            ReadValues(true, false);
    }

    /// <inheridoc />
    public async ValueTask SkipAsync(int skipCount = 1, CancellationToken ct = default)
    {
        if (skipCount < 1)
            throw new ArgumentOutOfRangeException(nameof(skipCount));

        for (var i = 0; i < skipCount; i++)
            await ReadValuesAsync(true, false, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool MoveNext(bool trimValues)
    {
        if (IsDisposed || EndOfStream)
            return false;

        try
        {
            ReadValues(false, trimValues);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool MoveNext() => MoveNext(TrimsValues);

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(bool trimValues, CancellationToken ct = default)
    {
        if (IsDisposed || EndOfStream)
            return false;

        try
        {
            await ReadValuesAsync(false, trimValues, ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken ct = default) =>
        await MoveNextAsync(TrimsValues, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync() =>
        await MoveNextAsync(TrimsValues, CancellationToken.None);

    /// <inheritdoc />
    public bool TryGetValue(int index, out string value)
    {
        value = string.Empty;
        if (Values is null || index >= Values.Count || index < 0)
            return false;

        value = Values[index];
        return true;
    }

    /// <inheritdoc />
    public void Reset() => throw new NotSupportedException("Reset is not supported by CSV readers.");

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(alsoManaged: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        var result = DisposeAsync(alsoManaged: true);
        GC.SuppressFinalize(this);
        return result;
    }


    /// <inheritdoc />
    public override string ToString() =>
        $"{GetType()}: {Count} records read.";

    /// <inheritdoc />
    public IEnumerator<TLine> GetEnumerator() => this;

    /// <inheritdoc />
    public IAsyncEnumerator<TLine> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this;

    /// <summary>
    /// Parses a set of literals from the underlying stream, and when the skip parameter is set to false,
    /// increments the <see cref="Count"/> property by one and sets the <see cref="Values"/> property
    /// when the operation succeeds.
    /// </summary>
    /// <param name="isSkipping">True if the <see cref="Count"/> and <see cref="Values"/> properties will not be set.</param>
    /// <param name="trimValues">Determines if values should be trimmed.</param>
    protected void ReadValues(bool isSkipping, bool trimValues)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(CsvReader));

        if (EndOfStream)
            throw new EndOfStreamException("Unable to read past the end of the stream.");

        var result = ParseRecord(_reader, trimValues, EscapeChar, SeparatorChar);

        if (isSkipping)
            return;

        Values = result;
        _count.Increment();
    }

    /// <summary>
    /// Asynchronously parses a set of literals from the underlying stream, and when the skip parameter is set to false,
    /// increments the <see cref="Count"/> property by one and sets the <see cref="Values"/> property
    /// when the operation succeeds.
    /// </summary>
    /// <param name="isSkipping">True if the <see cref="Count"/> and <see cref="Values"/> properties will not be set.</param>
    /// <param name="trimValues">Determines if values should be trimmed.</param>
    /// <param name="ct">The optional cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    protected async ValueTask ReadValuesAsync(bool isSkipping, bool trimValues, CancellationToken ct = default)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(CsvReader));

        if (EndOfStream)
            throw new EndOfStreamException("Unable to read past the end of the stream.");

        var result = await ParseRecordAsync(_reader, trimValues, EscapeChar, SeparatorChar, ct).ConfigureAwait(false);

        if (isSkipping)
            return;

        Values = result;
        _count.Increment();
    }

    /// <summary>
    /// Disposes this instance optionally disposing
    /// of managed objects.
    /// </summary>
    /// <param name="alsoManaged">If managed objects should also be disposed of.</param>
    protected virtual void Dispose(bool alsoManaged)
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        if (alsoManaged)
            _reader.Dispose();
    }

    /// <summary>
    /// Disposes this instance optionally disposing
    /// of managed objects.
    /// </summary>
    /// <param name="alsoManaged">If managed objects should also be disposed of.</param>
    protected virtual ValueTask DisposeAsync(bool alsoManaged)
    {
        Dispose(alsoManaged);
        return ValueTask.CompletedTask;
    }


    /// <summary>
    /// Reads the next line of text from the provided text reader.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="line">The line that was read as a span.</param>
    /// <returns>True if line read succeeds (even if it is empty). False if we have reached the end of the stream.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadLine(TextReader reader, [MaybeNullWhen(false)] out ReadOnlySpan<char> line)
    {
        line = default;
        var textLine = reader.ReadLine();
        if (textLine is null)
            return false;

        line = textLine.AsSpan();
        return true;
    }

    /// <summary>
    /// Given a current read state, provides the following read state based on the characters provided
    /// in the read only span of characters.
    /// </summary>
    /// <param name="line">The characters to parse.</param>
    /// <param name="currentState">The current state of the parser.</param>
    /// <param name="values">The cumulative set of values.</param>
    /// <param name="currentValue">A string builder that holds a cumulative current field value.</param>
    /// <param name="trimValues">Option: trims characters.</param>
    /// <param name="escapeChar">Option: the field escape character.</param>
    /// <param name="separatorChar">Option: the field separator character.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadState ParseToNextReadState(ReadOnlySpan<char> line, ReadState currentState, List<string> values, StringBuilder currentValue,
        bool trimValues, char escapeChar, char separatorChar)
    {
        for (var charIndex = 0; charIndex < line.Length; charIndex++)
        {
            // Get the current and next character
            var currentChar = line[charIndex];
            var nextChar = GetNextChar(charIndex, line);

            // Perform logic based on state and decide on next state
            switch (currentState)
            {
                case ReadState.WaitingForNewField:
                    currentValue.Clear();

                    if (currentChar == escapeChar)
                    {
                        currentState = ReadState.PushingQuoted;
                        continue;
                    }

                    if (currentChar == separatorChar)
                    {
                        values.Add(trimValues ? currentValue.ToString().Trim() : currentValue.ToString());
                        currentState = ReadState.WaitingForNewField;
                        continue;
                    }

                    currentValue.Append(currentChar);
                    currentState = ReadState.PushingNormal;
                    continue;

                case ReadState.PushingNormal:
                    // Handle field content delimiter separator char
                    if (currentChar == separatorChar)
                    {
                        currentState = ReadState.WaitingForNewField;
                        values.Add(trimValues ? currentValue.ToString().Trim() : currentValue.ToString());
                        currentValue.Clear();
                        continue;
                    }

                    // Handle double quote escaping
                    if (currentChar == escapeChar && nextChar == escapeChar)
                    {
                        // advance 1 character now. The loop will advance one more.
                        currentValue.Append(currentChar);
                        charIndex++;
                        continue;
                    }

                    currentValue.Append(currentChar);
                    break;

                case ReadState.PushingQuoted:
                    // Handle field content delimiter by ending double quotes
                    if (currentChar == escapeChar && (!nextChar.HasValue || nextChar != escapeChar))
                    {
                        currentState = ReadState.PushingNormal;
                        continue;
                    }

                    // Handle double quote escaping
                    if (currentChar == escapeChar && nextChar == escapeChar)
                    {
                        // advance 1 character now. The loop will advance one more.
                        currentValue.Append(currentChar);
                        charIndex++;
                        continue;
                    }

                    currentValue.Append(currentChar);
                    break;
            }
        }

        // determine if we need to continue reading a new line if it is part of the quoted
        // field value
        if (currentState == ReadState.PushingQuoted)
        {
            // we need to add the new line sequence to the output of the field
            // because we were pushing a quoted value
            currentValue.Append(Environment.NewLine);
        }
        else
        {
            // push anything that has not been pushed (flush) into a last value
            values.Add(trimValues ? currentValue.ToString().Trim() : currentValue.ToString());
            currentValue.Clear();

            // stop reading more lines we have reached the end of the CSV record
            currentState = ReadState.CommitRecord;
        }

        return currentState;
    }

    /// <summary>
    /// Parses standard CSV text into an array of strings.
    /// Note that quoted values might have new line sequences in them. Field values will contain such sequences.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="trimValues">Determines if values should be trimmed.</param>
    /// <param name="escapeChar">The escape character.</param>
    /// <param name="separatorChar">The separator character.</param>
    /// <returns>An array of the specified element type containing copies of the elements of the ArrayList.</returns>
    private static IReadOnlyList<string> ParseRecord(TextReader reader, bool trimValues, char escapeChar, char separatorChar)
    {
        var values = new List<string>(64);
        var currentValue = new StringBuilder(256);
        var currentState = ReadState.WaitingForNewField;

        while (currentState != ReadState.CommitRecord && TryReadLine(reader, out var line))
            currentState = ParseToNextReadState(line, currentState, values, currentValue, trimValues, escapeChar, separatorChar);

        // If we ended up pushing quoted and no closing quotes we might
        // have additional text in it
        if (currentValue.Length > 0)
            values.Add(trimValues ? currentValue.ToString().Trim() : currentValue.ToString());

        return values;
    }

    /// <summary>
    /// Asynchronously parses standard CSV text into an array of strings.
    /// Note that quoted values might have new line sequences in them. Field values will contain such sequences.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="trimValues">Determines if values should be trimmed.</param>
    /// <param name="escapeChar">The escape character.</param>
    /// <param name="separatorChar">The separator character.</param>
    /// <param name="ct">The optional Cancellation Token.</param>
    /// <returns>An array of the specified element type containing copies of the elements of the ArrayList.</returns>
    private static async ValueTask<IReadOnlyList<string>> ParseRecordAsync(TextReader reader, bool trimValues, char escapeChar, char separatorChar, CancellationToken ct = default)
    {
        var values = new List<string>(64);
        var currentValue = new StringBuilder(256);
        var currentState = ReadState.WaitingForNewField;

        while (currentState != ReadState.CommitRecord && !ct.IsCancellationRequested)
        {
            var textLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (textLine is null)
                break;

            currentState = ParseToNextReadState(textLine.AsSpan(), currentState, values, currentValue, trimValues, escapeChar, separatorChar);
        }

        // If we ended up pushing quoted and no closing quotes we might
        // have additional text in it
        if (currentValue.Length > 0)
            values.Add(trimValues ? currentValue.ToString().Trim() : currentValue.ToString());

        return values;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char? GetNextChar(int charIndex, ReadOnlySpan<char> line) =>
        charIndex < line.Length - 1
            ? line[charIndex + 1]
            : default(char?);

    /// <summary>
    /// Defines the 3 different read states
    /// for the parsing state machine.
    /// </summary>
    private enum ReadState
    {
        WaitingForNewField,
        PushingNormal,
        PushingQuoted,
        CommitRecord
    }
}
