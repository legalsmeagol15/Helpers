# Helpers (C#)
**Wesley Oates**

This solution is designed to include some useful helpers solving commonly-encountered problems, like implementing 
parsing of command-line argument strings or graph searches.  Feel free to copy, modify, re-use, or flame to your 
your heart's content, though please do so with attribution.  I reserve the right to crib from any cool ideas that 
are implemented based on this code, as I extend it over time.

Some  highlights:

## Arguments

This is my argument-parsing implementation.  The idea is to define a class into which argument values can be written 
using automagic reflection, eliminating the need for tedious parsing of command line args in your apps.  (Really, this 
can be used to parse any `string[]`.  An obvious example would be HTTP header parsing.)  Parsing failures throw useful 
and informative exceptions.  The properties of each argument can be specified in the parameter class using handy 
Attributes, with very little complex code necessary.

Right now, the features include:
- Smart parsing of bool and key/value arguments.  A key/value pair will be provided in the form `key=value` or 
`key:value`, whereas a bool will be a solitary argument in the form `enable`.
- Automatic type detection of values for key/value pair arguments.
- Specify alternative groups.  If no alternative groups are satisfied, an exception is thrown.
- Specify aliases for arguments, choosing whether the arguments are case-sensitive or not.
- Specify individual single-letter flags, or parse them combined.  An argument will be parsed as flag if it is 
preceded by a hyphen, as in `-xvf`.
- Argument order can be indeterminate.

An usage example would be the following parameter class:

```
public class SimpleOptions
{
	[Alias("active", false)]
	[Group("GroupA", true)]
	public bool Enabled;

	[Group("GroupA", true)]
	public string FirstName;

	[Group("GroupA")]
	public string LastName;

	[Group("GroupA")]
	public int Age;
}
```

Now suppose you are parsing command line arguments in your app called `myApp.exe` like so, with the `Main()` method 
defined as follows:

```
C:\Users\WesleyOates\myApp\myApp active FirstName=John LastName=Doe Age=20
.
.
.
static int Main(string[] args) 
{
	...
	var opts = Arguments.Options.Parse<SimpleOptions>(args);
	...
}
```

_Hey presto!_  That is all you need to have a `SimpleOptions` parsed from the given command line options.

## Artificial Intelligence

Meh.  Stick with Python for now.  I haven't gotten around to implementing these ones, mostly.

## DataStructures

I often encounter the need for a particular data structure that does _X_, and just as often, I realize that data 
structures can be implemented in a general or generic way.  I have built a collection here.  Some of the particularly 
interesting ones:

- `Deque<T>` - Why wasn't this part of the C# standard libraries?  Does it exist under some other moniker?  Well, 
sometimes the implementation is more fun than the use anyway.  Enjoy that one here.
- `DynamicLinkedList<T>` - Just like your basic `LinkedList<T>`, but with a reference to a `DynamicLinkedListNode<T>`, 
you can delete, add before, or add after in an _O(1)_ operation.
- `IntervalSet<T> where T : IComparable<T>` - A weird data structure whose purpose is to maintain the true/false 
whether the set contains some `T` item.  Items can be added or removed in contiguous runs.  For example, in an 
`IntervalSet<int>`, all the ints between 20 and 2,000,000 can be added in a single near-_O(1)_ operation.  Obviously, 
the structure cheats by storing only where the run begins and ends, so the contents do not _really_ exist meaningfully 
on the set.  But you can do cool boolean operations between two `IntervalSet<T>` operations.
- `PipelineWorker` and `RedoWorker`.  Suppose you have a task that needs to be performed asynchronously, but you don't 
care particularly when it will be completed.  In the meantime, you might enqueue more work to be done as soon as the 
first item is complete.  These inherits of the `BackgroundWorker` might be useful.  This is the older style of multi-
threading in C# so I don't know how they might compare with `Task`s or `async/await`.  But they're event-driven and 
easy to use.

## Dependency

This project takes the usual notion of variables, but turns it on its head.  In programming generally, the value of 
a variable is expected to remain what it is set to be, even though its semantic significance may be the function 
output of the interaction of other variables.  In other words, if `triangle_height` is some number whose semantic 
value is supposed to equal the sine of 'theta', changing 'theta' will not automatically update the value of 
`triangle_height`.

This is useful for the situation where you want an entirely system to have a coherent state, but you don't want to 
have to recalculate the state in its entirety with every little change.  Only the parts affected are updated in 
response to any particular change.
