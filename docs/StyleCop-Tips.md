# StyleCop Analyzers

## Correct Order of Elements

According to [StyleCop 4.5 doc](http://stylecop.soyuz5.com/SA1201.html), elements at the file root level or within a
namespace must be positioned in the following order:

- Extern Alias Directives
- Using Directives
- Namespaces
- Delegates
- Enums
- Interfaces
- Structs
- Classes

Within a class, struct, or interface, elements must be positioned in the following order:

- Constant fields
- Fields
- Constructors
- Finalizers (Destructors)
- Delegates
- Events
- Enums
- Interfaces
- Properties
- Indexers
- Methods
- Structs
- Classes

Adjacent elements of the same type must be positioned in the following order by access level:

- public
- internal
- protected internal
- protected
- private

Within each access group follow the order of:

- static
- non-static

Then order each group of fields in:

- readonly
- non-readonly
