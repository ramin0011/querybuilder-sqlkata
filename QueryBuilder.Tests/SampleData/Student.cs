using System;
using System.Collections.Generic;

namespace SqlKata.Tests.SampleData;

public class Student
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Book> Books { get; set; }
}

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid? StudentId { get; set; }
}
