# ManiaSchema

XML schemas (XSD 1.0) for **Manialink** pages, **ManiaCode** files, **match settings** and **guest lists** in TrackMania Forever and ManiaPlanet, and for ManiaPlanet's **MoodSetting** and **MoodBlender** files. They provide autocompletion, documentation and validation, and are derived from static analysis of the game executables.

| Format | Root element | TrackMania Forever (2.11.26) | ManiaPlanet (2019-11-19) |
| --- | --- | --- | --- |
| Manialink | `<manialink>` | [manialink.xsd](TmForever/manialink.xsd) · [examples](TmForever/examples/manialink) | [manialink.xsd](ManiaPlanet/manialink.xsd) · [examples](ManiaPlanet/examples/manialink) |
| ManiaCode | `<maniacode>` | [maniacode.xsd](TmForever/maniacode.xsd) · [examples](TmForever/examples/maniacode) | [maniacode.xsd](ManiaPlanet/maniacode.xsd) · [examples](ManiaPlanet/examples/maniacode) |
| Match settings | `<playlist>` | [matchsettings.xsd](TmForever/matchsettings.xsd) · [example](TmForever/examples/matchsettings) | [matchsettings.xsd](ManiaPlanet/matchsettings.xsd) · [example](ManiaPlanet/examples/matchsettings) |
| Guest list | `<guestlist>` | [guestlist.xsd](TmForever/guestlist.xsd) · [example](TmForever/examples/guestlist) | [guestlist.xsd](ManiaPlanet/guestlist.xsd) · [example](ManiaPlanet/examples/guestlist) |
| MoodSetting | `<Mood>` | - | [moodsetting.xsd](ManiaPlanet/moodsetting.xsd) · [examples](ManiaPlanet/examples/moodsetting) |
| MoodBlender | `<MoodBlender>` | - | [moodblender.xsd](ManiaPlanet/moodblender.xsd) · [example](ManiaPlanet/examples/moodblender) |

## Usage

Reference the schema for your game and format on the root element:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<manialink version="3"
           xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
           xsi:noNamespaceSchemaLocation="https://raw.githubusercontent.com/BigBang1112/mania-schema/main/ManiaPlanet/manialink.xsd">
  <label pos="0 0" halign="center" text="Hello, ManiaPlanet!"/>
</manialink>
```

A relative path to a downloaded `.xsd` works too. Editors with XSD support will pick it up, for example VS Code with the [XML extension](https://marketplace.visualstudio.com/items?itemName=redhat.vscode-xml).

## Validate with .NET

```csharp
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

var schemas = new XmlSchemaSet();
schemas.Add(null, "ManiaPlanet/manialink.xsd");

// Throws an XmlException if the XML is not well-formed or has a DOCTYPE
using var reader = XmlReader.Create("page.xml");
var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

// .NET does not validate elements in unknown namespaces, so the root must not have one
var valid = doc.Root!.Name.Namespace == XNamespace.None;

doc.Validate(schemas, (_, e) =>
{
    valid = false;
    Console.WriteLine($"({e.Exception.LineNumber},{e.Exception.LinePosition}): {e.Message}");
});

Console.WriteLine(valid ? "Valid" : "Invalid");
```

## Tests

CI compiles every schema and validates every example against the schema named in its `xsi:noNamespaceSchemaLocation`, with both .NET and libxml2. To run them locally from the repository root:

```sh
dotnet run tests/validate.cs   # .NET 10 SDK
sh tests/xmllint.sh            # needs xmllint (libxml2-utils)
```

## Good to know

- The schemas are stricter than the games, which silently ignore many of the mistakes reported here.
- The schemas have no target namespace, so remove any default `xmlns="..."` before validating.
- Not every rule can be expressed in XSD 1.0. For example, misspelled attributes on ManiaPlanet controls and frames are not reported. The comment at the top of each schema explains its limitations.
