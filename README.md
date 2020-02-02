# Shaman.ValueString
Allocation-free methods for string manipulation.

`ValueString` is a struct type that represents a portion of a `string`.

Supported features:
* `Concat`, `IndexOf`, `LastIndexOf`, `Equals`, `StartsWith`, `EndsWith`, `Substring`, `Trim`
* `ParseInt32`, `ParseInt64`, `ParseUInt32`, `ParseUInt64` and their `TryParseXxxx` equivalents.
* Allocation-free `Split(char)`. You can optionally pass a `ref` array to be recycled
* `SubstringCached`/`ToStringCached` for strings and `StringBuilder`s: uses a cache of recently-allocated strings, useful when parsing many possibly repeated strings.
* Specialized versions of `AppendFast` and `Write` for `StringBuilder`s and `TextWriter`s with numeric inputs.
* `MultiValueStringBuilder`, creates ValueString from arbitrary `char[]` data. A large `string` is internally used to batch writes.
* `ValueStringStreamReader`, useful for reading text or CSV files without allocating a new string for every line.

## Example usage
```csharp
using Shaman.Runtime;

int num = ValueString.ParseInt32("example-12".AsValueString().Substring(8)) // No allocations

var mv = new MultiValueStringBuilder(4096);
ValueString a = mv.CreateValueString(new char[] { 'h', 'e', 'l', 'l', 'o'});
ValueString b = mv.CreateValueString(new StringBuilder("world"));
// ValueString a == "hello"
// ValueString b == "world"
// Now, mv contains an internal string "helloworld\0\0\0\0\0\0\0\0…".
// Further ValueString creations will be appended to this physical string (using unsafe code).
mv.DestroyPreviousValueStrings(); // Reuses the same string, overwriting old values.
                                  // Make sure you haven't stored old ValueStrings.

using (var csv = new ValueStringStreamReader(stream, Encoding.UTF8, mv))
{
    ValueString[] arr = null;
    while (true)
    {
        mv.DestroyPreviousValueStrings();
        var line = csv.ReadLine();
        if (line == null) break;
        line.Value.Split('\t', StringSplitOptions.None, ref arr);

        ulong id;
        Utils.TryParseUInt64(arr[0], out id);
        // Process the remaining fields
    }
}
```


