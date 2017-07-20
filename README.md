# <img src="icon.png" style="height: 32px; vertical-align: text-bottom" /> EntityFramework.ChangeTrackingExtensions
 An extension for EntityFramework and EntityFrameworkCore that provides Audit Logging, Concurrency Checks, storing complex types as JSON and storing history of all changes from `DbContext` to Transaction Log.

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/gnaeus/EntityFramework.ChangeTrackingExtensions/master/LICENSE)
[![NuGet version](https://img.shields.io/nuget/v/EntityFramework.ChangeTrackingExtensions.svg)](https://www.nuget.org/packages/EntityFramework.ChangeTrackingExtensions)
[![NuGet version](https://img.shields.io/nuget/v/EntityFrameworkCore.ChangeTrackingExtensions.svg)](https://www.nuget.org/packages/EntityFrameworkCore.ChangeTrackingExtensions)

## Documentation
 * [JSON Complex Types](#ef-json-field)
 * [Auditable Entities](#ef-auditable-entities)
 * [Concurrency Checkes](#ef-auditable-entities)
 * [Transaction Logs](#ef-transaction-logs)
 * [Extensions (EF 6 only)](#ef-6-only)

<br>

### <a name="ef-json-field"></a> JSON Complex Types
There is an utility struct named `JsonField`, that helps to persist any Complex Type as JSON string in single table column.

```cs
struct JsonField<TValue>
    where TValue : class
{
    public string Json { get; set; }
    public TValue Value { get; set; }
}
```

Usage:
```cs
using EntityFrameworkCore.ChangeTrackingExtensions;

class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Login { get; set; }

    private JsonField<Address> _address;
    // used by EntityFramework
    public string AddressJson
    {
        get { return _address.Json; }
        set { _address.Json = value; }
    }
    // used by application code
    public Address Address
    {
        get { return _address.Value; }
        set { _address.Value = value; }
    }

    // collection initialization by default
    private JsonField<ICollection<string>> _phones = new HashSet<string>();
    public string PhonesJson
    {
        get { return _phones.Json; }
        set { _phones.Json = value; }
    }
    public ICollection<string> Phones
    {
        get { return _phones.Value; }
        set { _phones.Value = value; }
    }
}

[NotMapped]
class Address
{
    public string City { get; set; }
    public string Street { get; set; }
    public string Building { get; set; }
}
```

If we update such JSON properties the following SQL is generated during `SaveChanges`:
```sql
UPDATE Users
SET AddressJson = '{"City":"Moscow","Street":"Arbat","Building":"10"}',
    PhonesJson = '["+7 (123) 456-7890","+7 (098) 765-4321"]'
WHERE Id = 1;
```

The `AddressJson` property is serialized from `Address` only when it accessed by EntityFramework.  
And the `Address` property is materialized from `AddressJson` only when EntityFramework writes to `AddressJson`.

If we want to initialize some JSON collection in entity consctuctor, for example:
```cs
class MyEntity
{
    public ICollection<MyObject> MyObjects { get; set; } = new HashSet<MyObject>();
}
```
We can use the following implicit conversion:
```cs
class MyEntity
{
    private JsonField<ICollection<MyObject>> _myObjects = new HashSet<MyObject>();
}
```
It uses the following implicit operator:
```cs
struct JsonField<TValue>
{
    public static implicit operator JsonField<TValue>(TValue value);
}
```

The only caveat is that `TValue` object should not contain reference loops.  
Because `JsonField` uses [Jil](https://github.com/kevin-montrose/Jil) (the fastest .NET JSON serializer) behind the scenes.

<br>
