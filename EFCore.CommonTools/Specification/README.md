## <a name="ef-specification"></a> Specification Pattern

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

<br>
