## Attaching ExpressionVisitor to IQueryable

With `.AsVisitable()` extension we can attach any `ExpressionVisitor` to `IQueryable<T>`.

```cs
public static IQueryable<T> AsVisitable<T>(
    this IQueryable<T> queryable, params ExpressionVisitor[] visitors);
```

## <a name="ef-querying"></a> Expandable extension methods for IQueryable

We can use extension methods for `IQueryable<T>` to incapsulate custom buisiness logic.  
But if we call these methods from `Expression<TDelegate>`, we get runtime error.

```cs
public static IQueryable<Post> FilterByAuthor(this IQueryable<Post> posts, int authorId)
{
    return posts.Where(p => p.AuthorId = authorId);
}

public static IQueryable<Comment> FilterTodayComments(this IQueryable<Comment> comments)
{
    DateTime today = DateTime.Now.Date;

    return comments.Where(c => c.CreationTime > today)
}

Comment[] comments = context.Posts
    .FilterByAuthor(authorId)   // it's OK
    .SelectMany(p => p.Comments
        .AsQueryable()
        .FilterTodayComments()) // will throw Error
    .ToArray();
```

With `.AsExpandable()` extension we can use extension methods everywhere.

```cs
Comment[] comments = context.Posts
    .AsExpandable()
    .FilterByAuthor(authorId)   // it's OK
    .SelectMany(p => p.Comments
        .FilterTodayComments()) // it's OK too
    .ToArray();
```

Expandable extension methods should return `IQueryable` and should have `[Expandable]` attribute.

```cs
[Expandable]
public static IQueryable<Post> FilterByAuthor(this IEnumerable<Post> posts, int authorId)
{
    return posts.AsQueryable().Where(p => p.AuthorId = authorId);
}

[Expandable]
public static IQueryable<Comment> FilterTodayComments(this IEnumerable<Comment> comments)
{
    DateTime today = DateTime.Now.Date;

    return comments.AsQueryable().Where(c => c.CreationTime > today)
}
```

<br>
