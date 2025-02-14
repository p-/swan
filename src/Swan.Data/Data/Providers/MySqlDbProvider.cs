﻿namespace Swan.Data.Providers;

internal class MySqlDbProvider : DbProvider
{
    public override IDbTypeMapper TypeMapper { get; } = new MySqlTypeMapper();

    public override string QuotePrefix => "`";

    public override string QuoteSuffix => "`";

    public override string SchemaSeparator => string.Empty;

    public override DbCommand CreateListTablesCommand(DbConnection connection)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        var database = connection.Database;

        return connection
            .BeginCommandText(
                $"SELECT `table_name` AS `Name`, '' AS `Schema` FROM `information_schema`.`tables` WHERE `table_schema` = {QuoteParameter(nameof(database))}")
            .EndCommandText()
            .SetParameter(nameof(database), database);
    }

    public override string? GetColumnDdlString(IDbColumnSchema column) => column is null
        ? throw new ArgumentNullException(nameof(column))
        : !TypeMapper.TryGetProviderTypeFor(column, out var providerType)
            ? default
            : column.IsIdentity && column.DataType.TypeInfo().IsNumeric
                ? $"{QuoteField(column.ColumnName),16} {providerType} NOT NULL AUTO_INCREMENT"
                : base.GetColumnDdlString(column);

    public override bool TryGetSelectLastInserted(IDbTableSchema table, out string? commandText)
    {
        commandText = null;
        if (table.IdentityKeyColumn is null || table.KeyColumns.Count != 1)
            return false;

        var quotedFields = string.Join(", ", table.Columns.Select(c => QuoteField(c.ColumnName)));
        var quotedTable = QuoteTable(table.TableName, table.Schema);
        var quotedKeyField = QuoteField(table.IdentityKeyColumn.ColumnName);

        commandText = $"SELECT {quotedFields} FROM {quotedTable} WHERE {quotedKeyField} = LAST_INSERT_ID() LIMIT 1";
        return true;
    }
}
