# MapTo
An object to object mapping generator using using [Roslyn source generator](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md).

## Installation
```
dotnet add package MapTo
```

## Usage
MapTo creates mappings during compile-time. To indicate which objects it needs to generate the mappings for, simply declare the class as `partial` and annotate it with `MapFrom` attribute.

```c#
[MapFrom(sourceType: typeof(App.Data.Models.User))]
public partial class UserViewModel 
{
    public string FirstName { get; }

    public string LastName { get; }
}
```

If `Data.Models.User` class is defined as follow:

```c#
namespace App.Data.Models
{
    public class User
    {
        public User(int id) => Id = id;
        
        public int Id { get; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
```

It will generate the following code:

```c#
// <auto-generated />
using System;

namespace App.ViewModels
{
    public partial class UserViewModel
    {
        public UserViewModel(App.Data.Models.User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            FirstName = user.FirstName;
            LastName = user.LastName;
        }

        public static UserViewModel From(App.Data.Models.User user)
        {
            return user == null ? null : new UserViewModel(user);
        }
    }

    public static partial class UserToUserViewModelExtensions
    {
        public static UserViewModel ToUserViewModel(this App.Data.Models.User user)
        {
            return user == null ? null : new UserViewModel(user);
        }
    }
}
```

Which makes it possible to get an instance of `UserViewModel` from one of the following ways:

```c#
var user = new User(id: 10) { FirstName = "John", LastName = "Doe" };

var vm = user.ToUserViewModel();

// OR
vm = new UserViewModel(user);

// OR
vm = UserViewModel.From(user);
```