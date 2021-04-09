# MapTo
[![Nuget](https://img.shields.io/nuget/v/mapto?logo=nuget)](https://www.nuget.org/packages/MapTo/)
![Publish Packages](https://github.com/mrtaikandi/MapTo/workflows/Publish%20Packages/badge.svg)

A convention based object to object mapper using [Roslyn source generator](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md).

MapTo is a library to programmatically generate the necessary code to map one object to another during compile-time, eliminating the need to use reflection to map objects and make it much faster in runtime. It provides compile-time safety checks and ease of use by leveraging extension methods.


## Installation
```
dotnet add package MapTo --prerelease
```

## Usage
MapTo relies on a set of attributes to instruct it on how to generate the mappings. To start, declare the destination class as `partial`  and annotate it with `MapFrom` attribute. As its name implies, `MapFrom` attribute tells the library what the source class you want to map from is.

```c#
using MapTo;

namespace App.ViewModels
{
    [MapFrom(typeof(App.Data.Models.User))]
    public partial class UserViewModel 
    {
        public string FirstName { get; }
    
        public string LastName { get; }
        
        [IgnoreProperty]
        public string FullName { get; set; }
    }
}
```

To get an instance of `UserViewModel` from the `User` class, you can use any of the following methods:

```c#
var user = new User(id: 10) { FirstName = "John", LastName = "Doe" };

var vm = user.ToUserViewModel(); // A generated extension method for User class.

// OR
vm = new UserViewModel(user); // A generated contructor.

// OR
vm = UserViewModel.From(user); // A generated factory method.
```

> Please refer to [sample console app](https://github.com/mrtaikandi/MapTo/tree/master/test/TestConsoleApp) for a more complete example.

## Available Attributes
### IgnoreProperty
By default, MapTo will include all properties with the same name (case-sensitive), whether read-only or not, in the mapping unless annotating them with the `IgnoreProperty` attribute.
```c#
[IgnoreProperty]
public string FullName { get; set; }
``` 

### MapProperty
This attribute gives you more control over the way the annotated property should get mapped. For instance, if the annotated property should use a property in the source class with a different name.

```c#
[MapProperty(SourcePropertyName = "Id")]
public int Key { get; set; }
```

### MapTypeConverter
A compilation error gets raised by default if the source and destination properties types are not implicitly convertible, but to convert the incompatible source type to the desired destination type, `MapTypeConverter` can be used.

This attribute will accept a type that implements `ITypeConverter<in TSource, out TDestination>` interface.

```c#
[MapFrom(typeof(User))]
public partial class UserViewModel
{
    public DateTimeOffset RegisteredAt { get; set; }
    
    [IgnoreProperty]
    public ProfileViewModel Profile { get; set; }
    
    [MapTypeConverter(typeof(IdConverter))]
    [MapProperty(SourcePropertyName = nameof(User.Id))]    
    public string Key { get; }

    private class IdConverter : ITypeConverter<int, string>
    {
        public string Convert(int source, object[] converterParameters) => $"{source:X}";
    }
}
```