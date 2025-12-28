using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlKata;

/// <summary>
/// A strongly-typed helper for building SQL queries based on a generic type T.
/// Automatically determines table name and column names from type properties.
/// </summary>
public class QueryHelper<T> : Query
{
    private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();
    private static readonly object _cacheLock = new object();

    private QueryHelper() : base(GetTableName())
    {
    }

    /// <summary>
    /// Creates a new QueryHelper instance for the generic type T
    /// </summary>
    public static QueryHelper<T> Create()
    {
        return new QueryHelper<T>();
    }

    /// <summary>
    /// Gets the table name from the generic type T
    /// </summary>
    private static string GetTableName()
    {
        var type = typeof(T);

        // Check for Table attribute (if you add one)
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name))
        {
            return tableAttr.Name;
        }

        // Use class name as table name
        return type.Name;
    }

    /// <summary>
    /// Gets all properties of type T that should be used as columns
    /// </summary>
    private static PropertyInfo[] GetProperties()
    {
        var type = typeof(T);

        lock (_cacheLock)
        {
            if (_propertyCache.ContainsKey(type))
            {
                return _propertyCache[type];
            }

            var properties = type.GetRuntimeProperties()
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToArray();

            _propertyCache[type] = properties;
            return properties;
        }
    }

    /// <summary>
    /// Gets the column name for a property
    /// </summary>
    private static string GetColumnName(PropertyInfo property)
    {
        var colAttr = property.GetCustomAttribute<ColumnAttribute>();
        return colAttr?.Name ?? property.Name;
    }

    /// <summary>
    /// Gets the column name for a property using expression
    /// </summary>
    private static string GetColumnName<TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            var property = memberExpression.Member as PropertyInfo;
            if (property != null)
            {
                return GetColumnName(property);
            }
        }
        else if (expression.Body is UnaryExpression unaryExpression &&
                 unaryExpression.Operand is MemberExpression unaryMember)
        {
            var property = unaryMember.Member as PropertyInfo;
            if (property != null)
            {
                return GetColumnName(property);
            }
        }

        throw new ArgumentException("Expression must be a property accessor", nameof(expression));
    }

    /// <summary>
    /// Gets all column names for type T
    /// </summary>
    public static string[] GetAllColumnNames()
    {
        return GetProperties().Select(GetColumnName).ToArray();
    }

    #region Select Methods

    /// <summary>
    /// Selects all columns from type T
    /// </summary>
    public new QueryHelper<T> Select()
    {
        var columns = GetAllColumnNames();
        base.Select(columns);
        return this;
    }

    /// <summary>
    /// Selects specific properties from type T using expressions
    /// </summary>
    public QueryHelper<T> Select(params Expression<Func<T, object>>[] properties)
    {
        var columns = properties.Select(GetColumnName).ToArray();
        base.Select(columns);
        return this;
    }

    /// <summary>
    /// Selects specific columns by name
    /// </summary>
    public new QueryHelper<T> Select(params string[] columns)
    {
        base.Select(columns);
        return this;
    }

    #endregion

    #region Where Methods

    /// <summary>
    /// Adds a where clause using a property expression
    /// </summary>
    public QueryHelper<T> Where<TProp>(Expression<Func<T, TProp>> property, string op, object value)
    {
        var columnName = GetColumnName(property);
        base.Where(columnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds a where clause with equality operator using a property expression
    /// </summary>
    public QueryHelper<T> Where<TProp>(Expression<Func<T, TProp>> property, object value)
    {
        return Where(property, "=", value);
    }

    /// <summary>
    /// Adds a where clause using an object with properties matching column names
    /// </summary>
    public new QueryHelper<T> Where(object constraints)
    {
        base.Where(constraints);
        return this;
    }

    /// <summary>
    /// Adds a where clause using column name
    /// </summary>
    public new QueryHelper<T> Where(string column, string op, object value)
    {
        base.Where(column, op, value);
        return this;
    }

    /// <summary>
    /// Adds a where clause using column name with equality
    /// </summary>
    public new QueryHelper<T> Where(string column, object value)
    {
        base.Where(column, value);
        return this;
    }

    /// <summary>
    /// Adds an OR where clause using a property expression
    /// </summary>
    public QueryHelper<T> OrWhere<TProp>(Expression<Func<T, TProp>> property, string op, object value)
    {
        var columnName = GetColumnName(property);
        base.OrWhere(columnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR where clause with equality using a property expression
    /// </summary>
    public QueryHelper<T> OrWhere<TProp>(Expression<Func<T, TProp>> property, object value)
    {
        return OrWhere(property, "=", value);
    }

    /// <summary>
    /// Adds a where null clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereNull<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.WhereNull(columnName);
        return this;
    }

    /// <summary>
    /// Adds a where not null clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereNotNull<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.WhereNotNull(columnName);
        return this;
    }

    /// <summary>
    /// Adds a where in clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereIn<TProp>(Expression<Func<T, TProp>> property, IEnumerable<TProp> values)
    {
        var columnName = GetColumnName(property);
        base.WhereIn(columnName, values);
        return this;
    }

    /// <summary>
    /// Adds a where not in clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereNotIn<TProp>(Expression<Func<T, TProp>> property, IEnumerable<TProp> values)
    {
        var columnName = GetColumnName(property);
        base.WhereNotIn(columnName, values);
        return this;
    }

    /// <summary>
    /// Adds a where between clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereBetween<TProp>(Expression<Func<T, TProp>> property, TProp lower, TProp higher)
    {
        var columnName = GetColumnName(property);
        base.WhereBetween(columnName, lower, higher);
        return this;
    }

    /// <summary>
    /// Adds a where like clause using a property expression
    /// </summary>
    public QueryHelper<T> WhereLike<TProp>(Expression<Func<T, TProp>> property, object value, bool caseSensitive = false)
    {
        var columnName = GetColumnName(property);
        base.WhereLike(columnName, value, caseSensitive);
        return this;
    }

    #endregion

    #region Insert Methods

    /// <summary>
    /// Inserts an instance of type T
    /// </summary>
    public QueryHelper<T> Insert(T entity, bool returnId = false)
    {
        base.AsInsert(entity, returnId);
        return this;
    }

    /// <summary>
    /// Inserts multiple instances of type T
    /// </summary>
    public QueryHelper<T> Insert(IEnumerable<T> entities)
    {
        var entitiesList = entities.ToList();
        if (!entitiesList.Any())
        {
            throw new InvalidOperationException("Cannot insert empty collection");
        }

        var properties = GetProperties();
        var columns = properties.Select(GetColumnName).ToList();

        var allValues = new List<IEnumerable<object>>();
        foreach (var entity in entitiesList)
        {
            var values = properties.Select(p => p.GetValue(entity)).ToList();
            allValues.Add(values);
        }

        base.AsInsert(columns, allValues);
        return this;
    }

    /// <summary>
    /// Inserts with specific columns and values
    /// </summary>
    public new QueryHelper<T> AsInsert(IEnumerable<string> columns, IEnumerable<object> values)
    {
        base.AsInsert(columns, values);
        return this;
    }

    /// <summary>
    /// Inserts with key-value pairs
    /// </summary>
    public new QueryHelper<T> AsInsert(IEnumerable<KeyValuePair<string, object>> values, bool returnId = false)
    {
        base.AsInsert(values, returnId);
        return this;
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates using an instance of type T (uses Key attribute to determine where clause)
    /// </summary>
    public QueryHelper<T> Update(T entity)
    {
        base.AsUpdate(entity);
        return this;
    }

    /// <summary>
    /// Updates specific columns with values
    /// </summary>
    public new QueryHelper<T> AsUpdate(IEnumerable<string> columns, IEnumerable<object> values)
    {
        base.AsUpdate(columns, values);
        return this;
    }

    /// <summary>
    /// Updates with key-value pairs
    /// </summary>
    public new QueryHelper<T> AsUpdate(IEnumerable<KeyValuePair<string, object>> values)
    {
        base.AsUpdate(values);
        return this;
    }

    /// <summary>
    /// Updates using an object with properties
    /// </summary>
    public new QueryHelper<T> AsUpdate(object data)
    {
        base.AsUpdate(data);
        return this;
    }

    /// <summary>
    /// Increments a column using property expression
    /// </summary>
    public QueryHelper<T> Increment<TProp>(Expression<Func<T, TProp>> property, int value = 1)
    {
        var columnName = GetColumnName(property);
        base.AsIncrement(columnName, value);
        return this;
    }

    /// <summary>
    /// Decrements a column using property expression
    /// </summary>
    public QueryHelper<T> Decrement<TProp>(Expression<Func<T, TProp>> property, int value = 1)
    {
        var columnName = GetColumnName(property);
        base.AsDecrement(columnName, value);
        return this;
    }

    #endregion

    #region Delete Methods

    /// <summary>
    /// Marks the query as a delete query
    /// </summary>
    public new QueryHelper<T> AsDelete()
    {
        base.AsDelete();
        return this;
    }

    #endregion

    #region Join Methods

    /// <summary>
    /// Joins with another table using type TJoin
    /// </summary>
    public QueryHelper<T> Join<TJoin>(string first, string second, string op = "=", string type = "inner join")
    {
        var joinTable = typeof(TJoin).Name;
        base.Join(joinTable, first, second, op, type);
        return this;
    }

    /// <summary>
    /// Joins with another table using type TJoin and property expressions
    /// </summary>
    public QueryHelper<T> Join<TJoin, TProp1, TProp2>(
        Expression<Func<T, TProp1>> leftProperty,
        Expression<Func<TJoin, TProp2>> rightProperty,
        string op = "=",
        string type = "inner join")
    {
        var joinTable = typeof(TJoin).Name;
        var leftColumn = GetColumnName(leftProperty);
        var rightColumn = GetColumnNameForType<TJoin,TProp2>(rightProperty);

        base.Join(joinTable, j => j.On($"{joinTable}.{rightColumn}", $"{GetTableName()}.{leftColumn}", op), type);
        return this;
    }

    /// <summary>
    /// Joins with a table by name
    /// </summary>
    public new QueryHelper<T> Join(string table, string first, string second, string op = "=", string type = "inner join")
    {
        base.Join(table, first, second, op, type);
        return this;
    }

    /// <summary>
    /// Joins with a table using a callback
    /// </summary>
    public new QueryHelper<T> Join(string table, Func<Join, Join> callback, string type = "inner join")
    {
        base.Join(table, callback, type);
        return this;
    }

    /// <summary>
    /// Left joins with another table using type TJoin
    /// </summary>
    public QueryHelper<T> LeftJoin<TJoin>(string first, string second, string op = "=")
    {
        return Join<TJoin>(first, second, op, "left join");
    }

    /// <summary>
    /// Left joins with another table using type TJoin and property expressions
    /// </summary>
    public QueryHelper<T> LeftJoin<TJoin, TProp1, TProp2>(
        Expression<Func<T, TProp1>> leftProperty,
        Expression<Func<TJoin, TProp2>> rightProperty,
        string op = "=")
    {
        return Join<TJoin, TProp1, TProp2>(leftProperty, rightProperty, op, "left join");
    }

    /// <summary>
    /// Right joins with another table using type TJoin
    /// </summary>
    public QueryHelper<T> RightJoin<TJoin>(string first, string second, string op = "=")
    {
        return Join<TJoin>(first, second, op, "right join");
    }

    /// <summary>
    /// Right joins with another table using type TJoin and property expressions
    /// </summary>
    public QueryHelper<T> RightJoin<TJoin, TProp1, TProp2>(
        Expression<Func<T, TProp1>> leftProperty,
        Expression<Func<TJoin, TProp2>> rightProperty,
        string op = "=")
    {
        return Join<TJoin, TProp1, TProp2>(leftProperty, rightProperty, op, "right join");
    }

    /// <summary>
    /// Cross joins with another table using type TJoin
    /// </summary>
    public QueryHelper<T> CrossJoin<TJoin>()
    {
        var joinTable = typeof(TJoin).Name;
        base.CrossJoin(joinTable);
        return this;
    }

    #endregion

    #region Having Methods

    /// <summary>
    /// Adds a having clause using a property expression
    /// </summary>
    public QueryHelper<T> Having<TProp>(Expression<Func<T, TProp>> property, string op, object value)
    {
        var columnName = GetColumnName(property);
        base.Having(columnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds a having clause with equality using a property expression
    /// </summary>
    public QueryHelper<T> Having<TProp>(Expression<Func<T, TProp>> property, object value)
    {
        return Having(property, "=", value);
    }

    /// <summary>
    /// Adds a having clause using column name
    /// </summary>
    public new QueryHelper<T> Having(string column, string op, object value)
    {
        base.Having(column, op, value);
        return this;
    }

    /// <summary>
    /// Adds a having clause with equality using column name
    /// </summary>
    public new QueryHelper<T> Having(string column, object value)
    {
        base.Having(column, value);
        return this;
    }

    /// <summary>
    /// Adds an OR having clause using a property expression
    /// </summary>
    public QueryHelper<T> OrHaving<TProp>(Expression<Func<T, TProp>> property, string op, object value)
    {
        var columnName = GetColumnName(property);
        base.OrHaving(columnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR having clause with equality using a property expression
    /// </summary>
    public QueryHelper<T> OrHaving<TProp>(Expression<Func<T, TProp>> property, object value)
    {
        return OrHaving(property, "=", value);
    }

    /// <summary>
    /// Adds a having null clause using a property expression
    /// </summary>
    public QueryHelper<T> HavingNull<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.HavingNull(columnName);
        return this;
    }

    /// <summary>
    /// Adds a having not null clause using a property expression
    /// </summary>
    public QueryHelper<T> HavingNotNull<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.HavingNotNull(columnName);
        return this;
    }

    /// <summary>
    /// Adds a having in clause using a property expression
    /// </summary>
    public QueryHelper<T> HavingIn<TProp>(Expression<Func<T, TProp>> property, IEnumerable<TProp> values)
    {
        var columnName = GetColumnName(property);
        base.HavingIn(columnName, values);
        return this;
    }

    /// <summary>
    /// Adds a having between clause using a property expression
    /// </summary>
    public QueryHelper<T> HavingBetween<TProp>(Expression<Func<T, TProp>> property, TProp lower, TProp higher)
    {
        var columnName = GetColumnName(property);
        base.HavingBetween(columnName, lower, higher);
        return this;
    }

    #endregion

    #region Aggregate Methods

    /// <summary>
    /// Adds a COUNT aggregate for a property
    /// </summary>
    public QueryHelper<T> Count<TProp>(Expression<Func<T, TProp>> property, string alias = null)
    {
        var columnName = GetColumnName(property);
        if (alias != null)
        {
            base.SelectRaw($"COUNT({columnName}) as {alias}");
        }
        else
        {
            base.SelectCount(columnName);
        }
        return this;
    }

    /// <summary>
    /// Adds a SUM aggregate for a property
    /// </summary>
    public QueryHelper<T> Sum<TProp>(Expression<Func<T, TProp>> property, string alias = null)
    {
        var columnName = GetColumnName(property);
        if (alias != null)
        {
            base.SelectRaw($"SUM({columnName}) as {alias}");
        }
        else
        {
            base.SelectSum(columnName);
        }
        return this;
    }

    /// <summary>
    /// Adds an AVG aggregate for a property
    /// </summary>
    public QueryHelper<T> Avg<TProp>(Expression<Func<T, TProp>> property, string alias = null)
    {
        var columnName = GetColumnName(property);
        if (alias != null)
        {
            base.SelectRaw($"AVG({columnName}) as {alias}");
        }
        else
        {
            base.SelectAvg(columnName);
        }
        return this;
    }

    /// <summary>
    /// Adds a MIN aggregate for a property
    /// </summary>
    public QueryHelper<T> Min<TProp>(Expression<Func<T, TProp>> property, string alias = null)
    {
        var columnName = GetColumnName(property);
        if (alias != null)
        {
            base.SelectRaw($"MIN({columnName}) as {alias}");
        }
        else
        {
            base.SelectMin(columnName);
        }
        return this;
    }

    /// <summary>
    /// Adds a MAX aggregate for a property
    /// </summary>
    public QueryHelper<T> Max<TProp>(Expression<Func<T, TProp>> property, string alias = null)
    {
        var columnName = GetColumnName(property);
        if (alias != null)
        {
            base.SelectRaw($"MAX({columnName}) as {alias}");
        }
        else
        {
            base.SelectMax(columnName);
        }
        return this;
    }

    #endregion

    #region OrderBy Methods

    /// <summary>
    /// Orders by a property expression ascending
    /// </summary>
    public QueryHelper<T> OrderBy<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.OrderBy(columnName);
        return this;
    }

    /// <summary>
    /// Orders by a property expression descending
    /// </summary>
    public QueryHelper<T> OrderByDesc<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.OrderByDesc(columnName);
        return this;
    }

    /// <summary>
    /// Orders by column names
    /// </summary>
    public new QueryHelper<T> OrderBy(params string[] columns)
    {
        base.OrderBy(columns);
        return this;
    }

    /// <summary>
    /// Orders by column names descending
    /// </summary>
    public new QueryHelper<T> OrderByDesc(params string[] columns)
    {
        base.OrderByDesc(columns);
        return this;
    }

    #endregion

    #region GroupBy Methods

    /// <summary>
    /// Groups by a property expression
    /// </summary>
    public QueryHelper<T> GroupBy<TProp>(Expression<Func<T, TProp>> property)
    {
        var columnName = GetColumnName(property);
        base.GroupBy(columnName);
        return this;
    }

    /// <summary>
    /// Groups by multiple property expressions
    /// </summary>
    public QueryHelper<T> GroupBy(params Expression<Func<T, object>>[] properties)
    {
        var columns = properties.Select(GetColumnName).ToArray();
        base.GroupBy(columns);
        return this;
    }

    /// <summary>
    /// Groups by column names
    /// </summary>
    public new QueryHelper<T> GroupBy(params string[] columns)
    {
        base.GroupBy(columns);
        return this;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Helper method to get column name from an expression for a different type
    /// </summary>
    ///
    private static string GetColumnNameForType<TJoin,Tprop>(Expression<Func<TJoin, Tprop>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            var property = memberExpression.Member as PropertyInfo;
            if (property != null)
            {
                var colAttr = property.GetCustomAttribute<ColumnAttribute>();
                return colAttr?.Name ?? property.Name;
            }
        }
        else if (expression.Body is UnaryExpression unaryExpression &&
                 unaryExpression.Operand is MemberExpression unaryMember)
        {
            var property = unaryMember.Member as PropertyInfo;
            if (property != null)
            {
                var colAttr = property.GetCustomAttribute<ColumnAttribute>();
                return colAttr?.Name ?? property.Name;
            }
        }

        throw new ArgumentException("Expression must be a property accessor", nameof(expression));
    }

    #endregion

    #region Chaining Methods - Override to return QueryHelper<T>

    public new QueryHelper<T> Limit(int value)
    {
        base.Limit(value);
        return this;
    }

    public new QueryHelper<T> Offset(int value)
    {
        base.Offset(value);
        return this;
    }

    public new QueryHelper<T> Take(int limit)
    {
        base.Take(limit);
        return this;
    }

    public new QueryHelper<T> Skip(int offset)
    {
        base.Skip(offset);
        return this;
    }

    public new QueryHelper<T> ForPage(int page, int perPage = 15)
    {
        base.ForPage(page, perPage);
        return this;
    }

    public new QueryHelper<T> Distinct()
    {
        base.Distinct();
        return this;
    }

    #endregion
}

/// <summary>
/// Attribute to specify a custom table name for a class
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string Name { get; }

    public TableAttribute(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }
        Name = name;
    }
}
