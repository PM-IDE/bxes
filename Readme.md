## This document describes the `bxes` format.

### Goals

The goal of creating the format is to provide a compact way of storing event logs in Process Mining field, especially in software field.
The main problem about software logs is their size, as each event can contain a lot of attributes and the number of events is big. Moreover,
some attributes values can be repeated many times (e.g. name of a method), thus, when using XES format, there will a lot of repetition, which leads
to enormous size of .xes files. `bxes` format aims to provide a binary representation of event logs, thus reducing the size and optimizing the process
of working with software event logs.

### Event log description

Event log is a sequence of traces, or, the `multiset` of traces. This indicates, that some traces can be repeated for several times.
An event log can contain meta-information, e.g. version of format, date of creation, etc.
Every trace is a sequence of events.
Trace may not contain any metadata as event log does.
Every event contains a set of attributes, where each attribute contains a name of attribute (`String`) and a value of the attribute (any of primitive types).
The core difference between `bxes` and other XML-like formats is that we do not allow complex data structures and nested tags.
E.g. the constructs like the following one are not allowed:

```xml
<event>
    <usermetadata>
        <string key = "concept-name" value = "John" />
        <int key = "age" value = "12" />
    </usermetadata>
</event>
```

In `bxes` every event contains a plain set of attributes, in other words every attribute is a pair `(name: String, value: PrimitiveType)`, and an event is
a set of such pairs.
Event may not contain metadata, as event log does, all information about an event should be stored in the set of its attributes.

### Core features of `bxes`

The bxes core features are:
- Aggressive reuse of attribute keys and values: instead of repeating them as in XES, the actual value will be stored once, while attributes in an event
  will reference those values.
- Aggressive reuse of attribute pairs
- Attribute values and each trace variant can be stored in a single file, or in separate files.
- Reuse of traces variants: i.e. two traces for a single trace variant will not be stored

### Type system

The following types are suported in bxes:
- `i32` (type id = 0, `4 bytes`)
- `i64` (type id = 1, `8 bytes`)
- `u32` (type id = 2, `4 bytes`)
- `u64` (type id = 3, `8 bytes`)
- `f64` (type id = 4, `8 bytes`)
- `String` (UTF-8 strings) (type id = 5, length bytes) + (length in bytes, `u64`)
- `bool` (type id = 6, `1 byte`)
- `date` (type id = 7, `8 bytes`), the date is UTC ticks.

Type id is one byte length. In case of string the length of a string in bytes is also serialized, the length of string takes 8 bytes.
Type id + additional type info (i.e. length of a string) forms a header of a value, followed by the actual value

### Single file format description

- The version of bxes is specified (`u32`) - `4 bytes`
- The number of values is written (`u64`) - `8 bytes`
- Then there is a sequence of values [(Header[type-id + metainfo], value)]
- Then there is a number of attribute key-values pairs (`u64`) - `8 bytes`
- After that there is a sequence of pairs (index(`u64`, `8 bytes`), index(`u64`, `8 bytes`)), which indicates the attributes key-value pairs.
- The number of event log metadata key-value pairs is written (`u64`, `8 bytes`)
- The event log metadata is written [key-value pair index (`u64`, `8 bytes`)]
- Then the number of traces variants is written (`u64`) - `8 bytes`
- Then the sequence of traces variants is written.
  Each variant is a pair (number_of_traces(`u64`, `8 bytes`), number_of_events(`u64`, `8 bytes`), [Event[(number_of_attributes(`u64`,` 8 bytes`), [key-value index (`u64`, `8 bytes`)])]])

 ### Multiple files format description

- Metadata file
    - The version of bxes is written (`u64`, `8 bytes`)
    - The number of metadata key-value pairs is written (`u64`, `8 bytes`)
    - The indices of key-value pairs in "Values file" is written [index(`u64`, `8 bytes`)]
- Values file
    - The version of bxes is written (`u64`, `8 bytes`)
    - The number of values is written (`u64`, `8 bytes`)
    - The values are written [(Header[type-id + metainfo], value)]
- Key-value pairs file
    - The version of bxes is written (`u64`, `8 bytes`)
    - The number of key-value pairs is written (`u64`, `8 bytes`)
    - The key-value pairs are written (index(`u64`, `8 bytes`), index(`u64`, `8 bytes`))
- Traces file
    - The version of bxes is written (`u64`, `8 bytes`)
    - The number of traces variants is written (`u64`, `8 bytes`)
    - Traces variants are written: (number_of_traces(`u64`, `8 bytes`), number_of_events(`u64`, `8 bytes`), [Event[(number_of_attributes(`u64`, `8 bytes`), [key-value index (`u64`, `8 bytes`)])]])

### Online event log transfer

The opportunity to divide event log into different files can help in online transferring of event logs. The core idea is that values, attribute key-value pairs, event log metadata and traces can be transferred independently.