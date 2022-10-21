﻿namespace Swan.Data.Schema;

/// <summary>
/// Represents minimal information about a field in a supported database provider.
/// </summary>
public interface IDbColumnSchema : ICloneable
{
    /// <summary>
    /// Gets the column name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the column ordinal (column position within the table).
    /// </summary>
    int Ordinal { get; }

    /// <summary>
    /// Gets the CLR type for the column.
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Gets the name of the provider-specific data type.
    /// </summary>
    string ProviderDataType { get; }

    /// <summary>
    /// Gets whether column values accept null values.
    /// </summary>
    bool AllowsDBNull { get; }

    /// <summary>
    /// Gets whether this column is part of the primary key.
    /// </summary>
    bool IsKey { get; }

    /// <summary>
    /// Gets whether the column is automatically incremented.
    /// </summary>
    bool IsAutoIncrement { get; }

    /// <summary>
    /// Gets whether the column is an expression, autoincremental, or
    /// automatically generated by the database. In other words,
    /// the field cannot be used with insert or update statements.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the precision for numeric fields.
    /// </summary>
    byte Precision { get; }

    /// <summary>
    /// Gets the scale for numeric fields.
    /// </summary>
    byte Scale { get; }

    /// <summary>
    /// Gets the maximum data byte or character length for the field.
    /// </summary>
    int MaxLength { get; }

    /// <summary>
    /// Gets the name of the index this column belongs to.
    /// </summary>
    string? IndexName { get; }

    /// <summary>
    /// Gets a value indicating whether this column is an identity primary key.
    /// </summary>
    bool IsIdentity => !AllowsDBNull && IsKey && IsAutoIncrement && DataType.TypeInfo().IsNumeric;
}
