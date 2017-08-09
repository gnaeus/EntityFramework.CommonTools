## Expandable extension-methods for `IQueryable<T>`

We can use extension-methods for `IQueryable<T>` to incapsulate custom buisiness logic.  
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
        .FilterTodayComments()) // runtime error
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

The only caveat is that our custom extension methods should have `[Expandable]` attribute.

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

## Attaching `ExpressionVisitor`s to `IQueryable<T>`

With `.AsVisitable()` extension we can attach any `ExpressionVisitor` to `IQueryable<T>`.

```cs
public static IQueryable<T> AsVisitable<T>(this IQueryable<T> queryable, params ExpressionVisitor[] visitors)
```

## Specification Pattern

Generic implementation of [Specification Pattern](https://en.wikipedia.org/wiki/Specification_pattern).

```cs
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);

    Expression<Func<T, bool>> ToExpression();
}

public class Specification<T> : ISpecification<T>
{
    public Specification(Expression<Func<T, bool>> predicate);
}
```

We can define named specifications:
```cs
class UserIsActiveSpec : Specification<User>
{
    public UserIsActiveSpec()
        : base(u => !u.IsDeleted) { }
}

class UserByLoginSpec : Specification<User>
{
    public UserByLoginSpec(string login)
        : base(u => u.Login == login) { }
}
```

Then we can combine specifications with conditional logic operators `&&`, `||` and `!`:
```cs
class CombinedSpec
{
    public CombinedSpec(string login)
        : base(new UserIsActiveSpec() && new UserByLoginSpec(login)) { }
}
```

Also we can test it:
```cs
var user = new User { Login = "admin", IsDeleted = false };
var spec = new CombinedSpec("admin");

Assert.IsTrue(spec.IsSatisfiedBy(user));
```

And use with `IEnumerable<T>`:

```cs
var users = Enumerable.Empty<User>();
var spec = new UserByLoginSpec("admin");

var admin = users.FirstOrDefault(spec.IsSatisfiedBy);

// or even
var admin = users.FirstOrDefault(spec);
```

Or even with `IQueryable<T>`:
```cs
var spec = new UserByLoginSpec("admin");

var admin = context.Users.FirstOrDefault(spec.ToExpression());

// or even
var admin = context.Users.FirstOrDefault(spec);

// and also inside Expression
var adminFiends = context.Users
    .AsVisitable(new SpecificationExpander())
    .Where(u => u.Firends.Any(spec.ToExpression()))
    .ToList();

// or even
var adminFiends = context.Users
    .AsVisitable(new SpecificationExpander())
    .Where(u => u.Firends.Any(spec))
    .ToList();
```
