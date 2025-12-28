using SqlKata.Compilers;
using SqlKata.Extensions;
using SqlKata.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SqlKata.Tests
{
    /// <summary>
    /// Test entities for QueryHelper tests
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Column("user_name")]
        public string Name { get; set; }

        public string Email { get; set; }

        public int Age { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int ViewCount { get; set; }

        public string Status { get; set; }

        [Ignore]
        public string TempData { get; set; }
    }

    [Table("orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Category { get; set; }
    }

    public class QueryHelperTests : TestSupport
    {
        #region Table Name Resolution Tests

        [Fact]
        public void TableNameFromClassName()
        {
            var query = QueryHelper<User>.Create().Select();
            var c = Compile(query);

            Assert.Contains("FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("FROM `User`", c[EngineCodes.MySql]);
        }

        [Fact]
        public void TableNameFromTableAttribute()
        {
            var query = QueryHelper<Order>.Create().Select();
            var c = Compile(query);

            Assert.Contains("FROM [orders]", c[EngineCodes.SqlServer]);
            Assert.Contains("FROM `orders`", c[EngineCodes.MySql]);
        }

        #endregion

        #region Select Tests

        [Fact]
        public void SelectAllColumns()
        {
            var query = QueryHelper<User>.Create().Select();
            var c = Compile(query);

            // Should select all columns except those with [Ignore] attribute
            Assert.Contains("[user_name]", c[EngineCodes.SqlServer]); // Column attribute
            Assert.Contains("[Email]", c[EngineCodes.SqlServer]);
            Assert.Contains("[Age]", c[EngineCodes.SqlServer]);
            Assert.DoesNotContain("[TempData]", c[EngineCodes.SqlServer]); // Ignored
        }

        [Fact]
        public void SelectSpecificPropertiesWithExpressions()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name, x => x.Email);
            var c = Compile(query);

            Assert.Equal("SELECT [user_name], [Email] FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Equal("SELECT `user_name`, `Email` FROM `User`", c[EngineCodes.MySql]);
        }

        [Fact]
        public void SelectByColumnNames()
        {
            var query = QueryHelper<User>.Create()
                .Select("user_name", "Email");
            var c = Compile(query);

            Assert.Equal("SELECT [user_name], [Email] FROM [User]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Where Tests

        [Fact]
        public void WhereWithPropertyExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Age, ">", 18);
            var c = Compile(query);

            Assert.Contains("WHERE [Age] > 18", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE `Age` > 18", c[EngineCodes.MySql]);
        }

        [Fact]
        public void WhereWithEqualityOperator()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Name, "John");
            var c = Compile(query);

            Assert.Contains("WHERE [user_name] = 'John'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereWithColumnAttribute()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Name, "John");
            var c = Compile(query);

            // Should use the column name from [Column("user_name")]
            Assert.Contains("[user_name]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void OrWhereWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Age, ">", 18)
                .OrWhere(x => x.Status, "admin");
            var c = Compile(query);

            Assert.Contains("WHERE [Age] > 18 OR [Status] = 'admin'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereNullWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereNull(x => x.DeletedAt);
            var c = Compile(query);

            Assert.Contains("WHERE [DeletedAt] IS NULL", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereNotNullWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereNotNull(x => x.DeletedAt);
            var c = Compile(query);

            Assert.Contains("WHERE [DeletedAt] IS NOT NULL", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereInWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereIn(x => x.Status, new[] { "active", "pending" });
            var c = Compile(query);

            Assert.Contains("WHERE [Status] IN ('active', 'pending')", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereNotInWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereNotIn(x => x.Status, new[] { "deleted", "banned" });
            var c = Compile(query);

            Assert.Contains("WHERE [Status] NOT IN ('deleted', 'banned')", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereBetweenWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereBetween(x => x.Age, 18, 65);
            var c = Compile(query);

            Assert.Contains("WHERE [Age] BETWEEN 18 AND 65", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereLikeWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .WhereLike(x => x.Name, "John%");
            var c = Compile(query);

            Assert.Contains("WHERE LOWER([user_name]) like 'john%'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereByColumnName()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where("Age", ">", 18);
            var c = Compile(query);

            Assert.Contains("WHERE [Age] > 18", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void WhereWithObject()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(new { Age = 25, Status = "active" });
            var c = Compile(query);

            Assert.Contains("WHERE [Age] = 25", c[EngineCodes.SqlServer]);
            Assert.Contains("[Status] = 'active'", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Insert Tests

        [Fact]
        public void InsertSingleEntity()
        {
            var user = new User
            {
                Name = "John Doe",
                Email = "john@example.com",
                Age = 25,
                Status = "active",
                TempData = "should be ignored"
            };

            var query = QueryHelper<User>.Create().Insert(user);
            var c = Compile(query);

            Assert.Contains("INSERT INTO [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("[user_name]", c[EngineCodes.SqlServer]); // Column attribute
            Assert.Contains("[Email]", c[EngineCodes.SqlServer]);
            Assert.Contains("[Age]", c[EngineCodes.SqlServer]);
            Assert.DoesNotContain("TempData", c[EngineCodes.SqlServer]); // Ignored
            Assert.Contains("'John Doe'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void InsertMultipleEntities()
        {
            var users = new[]
            {
                new User { Name = "John", Email = "john@example.com", Age = 25 },
                new User { Name = "Jane", Email = "jane@example.com", Age = 30 }
            };

            var query = QueryHelper<User>.Create().Insert(users);
            var c = Compile(query);

            Assert.Contains("INSERT INTO [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("'John'", c[EngineCodes.SqlServer]);
            Assert.Contains("'Jane'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void AsInsertWithKeyValuePairs()
        {
            var values = new Dictionary<string, object>
            {
                { "user_name", "John" },
                { "Email", "john@example.com" },
                { "Age", 25 }
            };

            var query = QueryHelper<User>.Create().AsInsert(values);
            var c = Compile(query);

            Assert.Contains("INSERT INTO [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("[user_name], [Email], [Age]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void UpdateEntity()
        {
            var user = new User
            {
                Id = 1,
                Name = "John Updated",
                Email = "john.updated@example.com",
                Age = 26
            };

            var query = QueryHelper<User>.Create().Update(user);
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("SET", c[EngineCodes.SqlServer]);
            // Should have WHERE clause from [Key] attribute
            Assert.Contains("WHERE [Id] = 1", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void AsUpdateWithObject()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Id, 1)
                .AsUpdate(new { Name = "John", Age = 26 });
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("SET", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Id] = 1", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void IncrementWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Id, 1)
                .Increment(x => x.ViewCount, 1);
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("[ViewCount] = [ViewCount] + 1", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void DecrementWithExpression()
        {
            var query = QueryHelper<Product>.Create()
                .Where(x => x.Id, 1)
                .Decrement(x => x.Stock, 5);
            var c = Compile(query);

            Assert.Contains("UPDATE [Product]", c[EngineCodes.SqlServer]);
            Assert.Contains("[Stock] = [Stock] + -5", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public void DeleteWithWhere()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Age, "<", 18)
                .AsDelete();
            var c = Compile(query);

            Assert.Contains("DELETE FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Age] < 18", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void DeleteByStatus()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Status, "deleted")
                .AsDelete();
            var c = Compile(query);

            Assert.Contains("DELETE FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'deleted'", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Join Tests

        [Fact]
        public void JoinWithTypeParameter()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Join<Order>("User.Id", "orders.UserId");
            var c = Compile(query);

            Assert.Contains("FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("INNER JOIN [orders]", c[EngineCodes.SqlServer]);
            Assert.Contains("ON [User].[Id] = [orders].[UserId]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void JoinWithPropertyExpressions()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Join<Order, int, int>(
                    u => u.Id,
                    o => o.UserId
                );
            var c = Compile(query);

            Assert.Contains("FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("INNER JOIN [Order]", c[EngineCodes.SqlServer]);
            Assert.Contains("ON ([Order].[UserId] = [User].[Id])", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void JoinWithPropertyNoSelectExpressions()
        {
            var query = QueryHelper<User>.Create()
                .Join<Order, int, int>(
                    u => u.Id,
                    o => o.UserId
                );
            var c = Compile(query);

            Assert.Contains("FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("INNER JOIN [Order]", c[EngineCodes.SqlServer]);
            Assert.Contains("ON ([Order].[UserId] = [User].[Id])", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void LeftJoinWithTypeParameter()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .LeftJoin<Order>("User.Id", "orders.UserId");
            var c = Compile(query);

            Assert.Contains("LEFT JOIN [orders]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void LeftJoinWithPropertyExpressions()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name)
                .LeftJoin<Order, int, int>(
                    u => u.Id,
                    o => o.UserId
                );
            var c = Compile(query);

            Assert.Contains("LEFT JOIN [orders]", c[EngineCodes.SqlServer]);
            Assert.Contains("ON [Id] = [UserId]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void RightJoinWithTypeParameter()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .RightJoin<Order>("User.Id", "Order.UserId");
            var c = Compile(query);

            Assert.Contains("RIGHT JOIN [Order]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void CrossJoinWithTypeParameter()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .CrossJoin<Order>();
            var c = Compile(query);

            Assert.Contains("CROSS JOIN [orders]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void JoinByTableName()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Join("orders", "User.Id", "orders.UserId");
            var c = Compile(query);

            Assert.Contains("INNER JOIN [orders]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Having Tests

        [Fact]
        public void HavingWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Age)
                .GroupBy(x => x.Age)
                .Having(x => x.Age, ">", 18);
            var c = Compile(query);

            Assert.Contains("GROUP BY [Age]", c[EngineCodes.SqlServer]);
            Assert.Contains("HAVING [Age] > 18", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void HavingWithEqualityExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Status)
                .Having(x => x.Status, "active");
            var c = Compile(query);

            Assert.Contains("HAVING [Status] = 'active'", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void OrHavingWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Age)
                .Having(x => x.Age, ">", 18)
                .OrHaving(x => x.Age, "<", 10);
            var c = Compile(query);

            Assert.Contains("HAVING [Age] > 18 OR [Age] < 10", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void HavingNullWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Status)
                .HavingNull(x => x.DeletedAt);
            var c = Compile(query);

            Assert.Contains("HAVING [DeletedAt] IS NULL", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void HavingInWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Status)
                .HavingIn(x => x.Status, new[] { "active", "pending" });
            var c = Compile(query);

            Assert.Contains("HAVING [Status] IN ('active', 'pending')", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void HavingBetweenWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Age)
                .HavingBetween(x => x.Age, 18, 65);
            var c = Compile(query);

            Assert.Contains("HAVING [Age] BETWEEN 18 AND 65", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Aggregate Tests

        [Fact]
        public void CountWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Count(x => x.Id, "totalUsers");
            var c = Compile(query);

            Assert.Contains("SELECT COUNT(Id) as totalUsers", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void SumWithExpression()
        {
            var query = QueryHelper<Order>.Create()
                .Sum(x => x.Amount, "totalAmount");
            var c = Compile(query);

            Assert.Contains("SELECT SUM(Amount) as totalAmount", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void AvgWithExpression()
        {
            var query = QueryHelper<Product>.Create()
                .Avg(x => x.Price, "avgPrice");
            var c = Compile(query);

            Assert.Contains("SELECT AVG(Price) as avgPrice", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void MinWithExpression()
        {
            var query = QueryHelper<Product>.Create()
                .Min(x => x.Price, "minPrice");
            var c = Compile(query);

            Assert.Contains("SELECT MIN(Price) as minPrice", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void MaxWithExpression()
        {
            var query = QueryHelper<Product>.Create()
                .Max(x => x.Price, "maxPrice");
            var c = Compile(query);

            Assert.Contains("SELECT MAX(Price) as maxPrice", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void MultipleAggregates()
        {
            var query = QueryHelper<Order>.Create()
                .Count(x => x.Id, "totalOrders")
                .Sum(x => x.Amount, "totalAmount")
                .Avg(x => x.Amount, "avgAmount")
                .GroupBy(x => x.Status);
            var c = Compile(query);

            Assert.Contains("COUNT(Id) as totalOrders", c[EngineCodes.SqlServer]);
            Assert.Contains("SUM(Amount) as totalAmount", c[EngineCodes.SqlServer]);
            Assert.Contains("AVG(Amount) as avgAmount", c[EngineCodes.SqlServer]);
            Assert.Contains("GROUP BY [Status]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region OrderBy and GroupBy Tests

        [Fact]
        public void OrderByWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .OrderByDesc(x => x.Name);
            var c = Compile(query);

            Assert.Contains("ORDER BY [user_name] DESC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void OrderByDescWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .OrderByDesc(x => x.Age);
            var c = Compile(query);

            Assert.Contains("ORDER BY [Age] DESC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void OrderByMultipleColumns()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .OrderBy(x => x.Age)
                .OrderByDesc(x => x.Name);
            var c = Compile(query);

            Assert.Contains("ORDER BY [Age], [user_name] DESC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void OrderByColumnNames()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .OrderByDesc("Age", "user_name");
            var c = Compile(query);

            Assert.Contains("ORDER BY [Age] DESC, [user_name] DESC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void GroupByWithExpression()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Status);
            var c = Compile(query);

            Assert.Contains("GROUP BY [Status]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void GroupByMultipleExpressions()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy(x => x.Status, x => x.Age);
            var c = Compile(query);

            Assert.Contains("GROUP BY [Status], [Age]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void GroupByColumnNames()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .GroupBy("Status", "Age");
            var c = Compile(query);

            Assert.Contains("GROUP BY [Status], [Age]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Chaining and Pagination Tests

        [Fact]
        public void LimitAndOffset()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Age, ">", 18)
                .OrderByDesc(x => x.Name)
                .Limit(10)
                .Offset(20);
            var c = Compile(query);

            Assert.Contains("WHERE [Age] > 18", c[EngineCodes.SqlServer]);
            Assert.Contains("ORDER BY [user_name] DESC", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [row_num] BETWEEN 21 AND 30", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void TakeAndSkip()
        {
            var query = QueryHelper<User>.Create()
                .Select()
                .Take(10)
                .Skip(20);
            var c = Compile(query);

            var offsetRowsFetchNextRowsOnly = """
                                              SELECT [Id], [user_name], [Email], [Age], [DeletedAt], [ViewCount], [Status] FROM (SELECT [Id], [user_name], [Email], [Age], [DeletedAt], [ViewCount], [Status], ROW_NUMBER() OVER (ORDER BY (SELECT 0)) AS [row_num] FROM [User]) AS [results_wrapper] WHERE [row_num] BETWEEN 21 AND 30
                                              """;
            Assert.Contains(offsetRowsFetchNextRowsOnly, c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void ForPage()
        {
            var query1 = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Age, ">", 20);

            var query2 = QueryHelper<User>.Create()
                .Select()
                .Where(x => x.Age, "<", 30)
                .ForPage(2, 15);

            var unionQuery = query1.Union(query2);

            var c = Compile(unionQuery);

            Assert.Equal(
                "SELECT [Id], [user_name], [Email], [Age], [DeletedAt], [ViewCount], [Status] FROM [User] WHERE [Age] > 20 UNION SELECT [Id], [user_name], [Email], [Age], [DeletedAt], [ViewCount], [Status] FROM (SELECT [Id], [user_name], [Email], [Age], [DeletedAt], [ViewCount], [Status], ROW_NUMBER() OVER (ORDER BY (SELECT 0)) AS [row_num] FROM [User] WHERE [Age] < 30) AS [results_wrapper] WHERE [row_num] BETWEEN 16 AND 30",
                c[EngineCodes.SqlServer]
            );


        }

        [Fact]
        public void Distinct()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Status)
                .Distinct();
            var c = Compile(query);

            Assert.Contains("SELECT DISTINCT [Status]", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Complex Query Tests

        [Fact]
        public void ComplexQueryWithMultipleClauses()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name, x => x.Email, x => x.Age)
                .Where(x => x.Age, ">", 18)
                .WhereNotNull(x => x.Email)
                .WhereIn(x => x.Status, new[] { "active", "pending" })
                .OrderBy(x => x.Name)
                .Limit(10);
            var c = Compile(query);

            Assert.Contains("SELECT [user_name], [Email], [Age]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Age] > 18", c[EngineCodes.SqlServer]);
            Assert.Contains("[Email] IS NOT NULL", c[EngineCodes.SqlServer]);
            Assert.Contains("[Status] IN ('active', 'pending')", c[EngineCodes.SqlServer]);
            Assert.Contains("ORDER BY [user_name] ASC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void ComplexQueryWithJoinAndAggregates()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name)
                .Join<Order, int, int>(
                    u => u.Id,
                    o => o.UserId
                )
                .Where(x => x.Status, "active")
                .GroupBy(x => x.Name)
                .Having(x => x.Id, ">", 0)
                .OrderBy(x => x.Name);
            var c = Compile(query);

            Assert.Contains("SELECT [user_name]", c[EngineCodes.SqlServer]);
            Assert.Contains("INNER JOIN [Order]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'active'", c[EngineCodes.SqlServer]);
            Assert.Contains("GROUP BY [user_name]", c[EngineCodes.SqlServer]);
            Assert.Contains("HAVING [Id] > 0", c[EngineCodes.SqlServer]);
            Assert.Contains("ORDER BY [user_name] ASC", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void ComplexQueryWithMultipleJoins()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name)
                .Join<Order>("User.Id", "orders.UserId")
                .LeftJoin<Product>("orders.ProductId", "Product.Id")
                .Where(x => x.Status, "active")
                .OrderBy(x => x.Name);
            var c = Compile(query);

            Assert.Contains("INNER JOIN [orders]", c[EngineCodes.SqlServer]);
            Assert.Contains("LEFT JOIN [Product]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void UpdateWithWhereClause()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Status, "pending")
                .AsUpdate(new { Status = "active", ViewCount = 0 });
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'pending'", c[EngineCodes.SqlServer]);
            Assert.Contains("SET", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void DeleteWithMultipleConditions()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Status, "deleted")
                .WhereNotNull(x => x.DeletedAt)
                .AsDelete();
            var c = Compile(query);

            Assert.Contains("DELETE FROM [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'deleted'", c[EngineCodes.SqlServer]);
            Assert.Contains("[DeletedAt] IS NOT NULL", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void Scenario_GetActiveUsersWithPagination()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name, x => x.Email, x => x.Age)
                .Where(x => x.Status, "active")
                .WhereNull(x => x.DeletedAt)
                .OrderBy(x => x.Name)
                .ForPage(1, 20);
            var c = Compile(query);

            Assert.Contains("SELECT TOP (20) [user_name], [Email], [Age]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'active'", c[EngineCodes.SqlServer]);
            Assert.Contains("[DeletedAt] IS NULL", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void Scenario_GetUserOrderStats()
        {
            var query = QueryHelper<Order>.Create()
                .Select(x => x.UserId)
                .Count(x => x.Id, "totalOrders")
                .Sum(x => x.Amount, "totalSpent")
                .Avg(x => x.Amount, "avgOrderAmount")
                .Where(x => x.Status, "completed")
                .GroupBy(x => x.UserId)
                .Having(x => x.Id, ">", 5);
            var c = Compile(query);

            Assert.Contains("COUNT(Id) as totalOrders", c[EngineCodes.SqlServer]);
            Assert.Contains("SUM(Amount) as totalSpent", c[EngineCodes.SqlServer]);
            Assert.Contains("AVG(Amount) as avgOrderAmount", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Status] = 'completed'", c[EngineCodes.SqlServer]);
            Assert.Contains("GROUP BY [UserId]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void Scenario_SearchProducts()
        {
            var query = QueryHelper<Product>.Create()
                .Select()
                .WhereLike(x => x.Name, "%laptop%")
                .WhereBetween(x => x.Price, 500, 2000)
                .Where(x => x.Stock, ">", 0)
                .OrderBy(x => x.Price)
                .Limit(50);
            var c = Compile(query);

            Assert.Contains("WHERE LOWER([Name]) like '%laptop%'", c[EngineCodes.SqlServer]);
            Assert.Contains("[Price] BETWEEN 500 AND 2000", c[EngineCodes.SqlServer]);
            Assert.Contains("[Stock] > 0", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void Scenario_BulkStatusUpdate()
        {
            var userIds = new[] { 1, 2, 3, 4, 5 };
            var query = QueryHelper<User>.Create()
                .WhereIn(x => x.Id, userIds)
                .AsUpdate(new { Status = "verified" });
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Id] IN (1, 2, 3, 4, 5)", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void Scenario_SoftDelete()
        {
            var query = QueryHelper<User>.Create()
                .Where(x => x.Id, 123)
                .AsUpdate(new { Status = "deleted", DeletedAt = DateTime.UtcNow });
            var c = Compile(query);

            Assert.Contains("UPDATE [User]", c[EngineCodes.SqlServer]);
            Assert.Contains("WHERE [Id] = 123", c[EngineCodes.SqlServer]);
        }

        #endregion

        #region Edge Cases and Validation Tests

        [Fact]
        public void GetAllColumnNames_ExcludesIgnoredProperties()
        {
            var columns = QueryHelper<User>.GetAllColumnNames();

            Assert.Contains("user_name", columns); // Column attribute
            Assert.Contains("Email", columns);
            Assert.Contains("Age", columns);
            Assert.DoesNotContain("TempData", columns); // Ignored
        }

        [Fact]
        public void ColumnAttributeTakesPrecedence()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name);
            var c = Compile(query);

            // Should use "user_name" from [Column] attribute, not "Name"
            Assert.Contains("[user_name]", c[EngineCodes.SqlServer]);
            Assert.DoesNotContain("[Name]", c[EngineCodes.SqlServer]);
        }

        [Fact]
        public void MultipleEnginesTest()
        {
            var query = QueryHelper<User>.Create()
                .Select(x => x.Name, x => x.Email)
                .Where(x => x.Age, ">", 18)
                .OrderBy(x => x.Name)
                .Limit(10);

            var c = Compile(query);

            // SQL Server
            Assert.Contains("SELECT TOP (10) [user_name], [Email] FROM [User] WHERE [Age] > 18 ORDER BY [user_name]", c[EngineCodes.SqlServer]);
            Assert.Contains("FROM [User]", c[EngineCodes.SqlServer]);

            // MySQL
            Assert.Contains("SELECT `user_name`, `Email`", c[EngineCodes.MySql]);
            Assert.Contains("FROM `User`", c[EngineCodes.MySql]);

            // PostgreSQL
            Assert.Contains("SELECT \"user_name\", \"Email\"", c[EngineCodes.PostgreSql]);
            Assert.Contains("FROM \"User\"", c[EngineCodes.PostgreSql]);
        }

        #endregion
    }
}

