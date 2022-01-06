﻿namespace Swan.Data.Context;

/// <summary>
/// Represents a table and schema that is bound to a specific connection.
/// </summary>
public interface ITableContext : IDbTableSchema, IConnected
{
    /// <summary>
    /// Builds a command and its parameters that can be used to insert
    /// records into this table.
    /// </summary>
    /// <param name="transaction">An optional transaction.</param>
    /// <returns>The command.</returns>
    IDbCommand BuildInsertCommand(IDbTransaction? transaction = null);

    /// <summary>
    /// Creates a command where an object found via its key column values
    /// and updated based on object property values.
    /// </summary>
    /// <param name="transaction">The optional transaction.</param>
    /// <returns>The command.</returns>
    IDbCommand BuildUpdateCommand(IDbTransaction? transaction = null);
}
