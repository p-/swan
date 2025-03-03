﻿namespace Swan.Data;

/// <summary>
/// Provides connection-specific metadata that is useful
/// when building commands or doing Type translation.
/// </summary>
public class DbProvider
{
    /// <summary>
    /// Creates a new instance of the <see cref="DbProvider"/> class.
    /// </summary>
    protected DbProvider()
    {
        // placeholder
    }

    /// <summary>
    /// Gets the translator between CLR types and DbTypes.
    /// </summary>
    public virtual IDbTypeMapper TypeMapper { get; } = new DbTypeMapper();

    /// <summary>
    /// Gets the prefix used to quote identifiers.
    /// </summary>
    public virtual string QuotePrefix => "[";

    /// <summary>
    /// Gets the suffix used to quote identifiers.
    /// </summary>
    public virtual string QuoteSuffix => "]";

    /// <summary>
    /// Gets the separator that goes between the schema and the table.
    /// </summary>
    public virtual string SchemaSeparator => ".";

    /// <summary>
    /// Gets the prefix used to name parameters on commands.
    /// </summary>
    public virtual string ParameterPrefix => "@";

    /// <summary>
    /// Gets the default schema name. For example in SQL Server, it will return dbo.
    /// Will return an empty string if no default schema is known.
    /// </summary>
    public virtual string DefaultSchemaName => string.Empty;

    /// <summary>
    /// Gets whether the provider supports schmas within databases. For example, SQL server.
    /// MySql or Sqlite currently do not. When <see cref="SchemaSeparator"/> is null or whitespace,
    /// the default implementation returns false.
    /// </summary>
    public virtual bool SupportsSchemas => !string.IsNullOrWhiteSpace(SchemaSeparator);

    /// <summary>
    /// Gets a default configuration for command timeout when
    /// creating commands via this API. Default is 60 seconds.
    /// </summary>
    public virtual TimeSpan DefaultCommandTimeout { get; private set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets a provider-specific schema column factory into which column metadata is read.
    /// </summary>
    public virtual Func<IDbColumnSchema> ColumnSchemaFactory { get; } = () => new DbColumnSchema();

    /// <summary>
    /// Fluet API for setting the default timeout for commands that are
    /// created via this API.
    /// </summary>
    /// <param name="timeout">The timeout to use.</param>
    /// <returns>This object for fluent API compatibility.</returns>
    public DbProvider WithDefaultCommandTimeout(TimeSpan timeout)
    {
        DefaultCommandTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Adds quotes around a table name along with an optional schema name.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <returns>A quoted table name.</returns>
    public virtual string QuoteTable(string tableName, string? schemaName = default) =>
        !string.IsNullOrWhiteSpace(schemaName) && SupportsSchemas
            ? string.Join(string.Empty,
                QuotePrefix,
                schemaName,
                QuoteSuffix,
                SchemaSeparator,
                QuotePrefix,
                tableName,
                QuoteSuffix)
            : $"{QuotePrefix}{tableName}{QuoteSuffix}";

    /// <summary>
    /// Adds quotes around a table name along with an optional schema name.
    /// </summary>
    /// <param name="table">The table schema.</param>
    /// <returns>A quoted table name.</returns>
    public virtual string QuoteTable(IDbTableSchema table) => table is not null
        ? QuoteTable(table.TableName, table.Schema)
        : throw new ArgumentNullException(nameof(table));

    /// <summary>
    /// Adds quotes around a field or column name.
    /// </summary>
    /// <param name="fieldName">The name of the field.</param>
    /// <returns>A quoted field name.</returns>
    public virtual string QuoteField(string fieldName) =>
        $"{QuotePrefix}{fieldName}{QuoteSuffix}";

    /// <summary>
    /// Adds the provider-specific parameter prefix to the specified parameter name.
    /// If the specified name already contains the parameter prefix, it simply returns
    /// the trimmed name.
    /// </summary>
    /// <param name="name">The name to add the parameter prefix to.</param>
    /// <returns>The quoted parameter name.</returns>
    public virtual string QuoteParameter(string name) => string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentNullException(nameof(name))
        : !string.IsNullOrWhiteSpace(ParameterPrefix) && name.StartsWith(ParameterPrefix, StringComparison.Ordinal)
        ? name.Trim()
        : $"{ParameterPrefix}{name.Trim()}";

    /// <summary>
    /// Removes the provider-specific parameter prefix from the specified parameter name.
    /// If the specified parameter name does not contain a parameter prefix, it simply returns
    /// the trimmed name.
    /// </summary>
    /// <param name="name">The name to remove the parameter prefix from.</param>
    /// <returns>The unquoted parameter name.</returns>
    public virtual string UnquoteParameter(string name) => string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentNullException(nameof(name))
        : !string.IsNullOrWhiteSpace(ParameterPrefix) && name.StartsWith(ParameterPrefix, StringComparison.Ordinal)
        ? new string(name.AsSpan()[ParameterPrefix.Length..]).Trim()
        : name.Trim();

    /// <summary>
    /// Gets a column DDL definition for the specific provider. Useful for building CREATE TABLE commands.
    /// </summary>
    /// <param name="column">The column schema.</param>
    /// <returns>The DDL string that represents the column.</returns>
    public virtual string? GetColumnDdlString(IDbColumnSchema column) => column is null
        ? throw new ArgumentNullException(nameof(column))
        : TypeMapper.TryGetProviderTypeFor(column, out var providerType)
        ? $"{QuoteField(column.ColumnName),16} {providerType}{(!column.AllowDBNull ? " NOT" : string.Empty)} NULL"
        : default;

    /// <summary>
    /// Gets a command that provides a list of table identifiers in the current database.
    /// The records must contain 2 columns, Name and Schema
    /// </summary>
    /// <returns>The command to be executed.</returns>
    public virtual DbCommand CreateListTablesCommand(DbConnection connection) =>
        throw new NotSupportedException("Connection provider does not support retrieving table names.");

    /// <summary>
    /// Gets a provider-specific DDL command for the given table.
    /// </summary>
    /// <param name="connection">The connection to build the command for.</param>
    /// <param name="table">The schema table.</param>
    /// <returns>The command.</returns>
    public virtual DbCommand CreateTableDdlCommand(DbConnection connection, IDbTableSchema table)
    {
        var (quotedTableName, orderedFields) = GetQuotedTableNameAndColumns(connection, table);
        var builder = new StringBuilder($"CREATE TABLE IF NOT EXISTS {quotedTableName} (\r\n")
            .Append(string.Join(",\r\n", orderedFields.Select(c => $"    {GetColumnDdlString(c)}").
                Where(c => !string.IsNullOrWhiteSpace(c)).ToArray()))
            .AppendLine("\r\n);");

        return connection
            .BeginCommandText(builder.ToString())
            .EndCommandText();
    }

    /// <summary>
    /// When overriden in a derived class, produces a SELECT statement that gets the last inserted
    /// record in a database table. Useful for insert commands.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="commandText">The output SELECT command text.</param>
    /// <returns>True if the provider supports producing the SELECT-back statement. False otherwise.</returns>
    public virtual bool TryGetSelectLastInserted(IDbTableSchema table, [MaybeNullWhen(false)] out string? commandText)
    {
        commandText = null;
        return false;
    }

    /// <summary>
    /// Provides a limit clause clause to skip and take a certain number of records.
    /// </summary>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    /// <returns>The SQL text that can be appended to the SQL statement.</returns>
    public virtual string GetLimitClause(int skip, int take) =>
        $"LIMIT {take} OFFSET {skip}";

    /// <inheritdoc/>
    protected (string quotedTableName, IOrderedEnumerable<IDbColumnSchema> orderedFields) GetQuotedTableNameAndColumns(
        DbConnection connection,
        IDbTableSchema table)
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table));

        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        if (table.ColumnCount <= 0)
            throw new InvalidOperationException("Cannot generate DDL code with no provided columns.");

        var quotedTableName = QuoteTable(table.TableName, table.Schema);
        var orderedFields = table.Columns.OrderBy(c => c.ColumnOrdinal).ThenBy(c => c.ColumnName);
        return (quotedTableName, orderedFields);
    }
}
