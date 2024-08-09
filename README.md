# vsLocalsDump2022

https://marketplace.visualstudio.com/items?itemName=MikaelAxblom.vsLocalsDump2022

An extension for Visual Studio 2022 that adds an option to dump a variable form locals in the debugger as JSON.
Since it is using data from the debugger and not the objects themselves the output is not the same as one would get from most of the popular libraries. The output is what you can see in the debugger.

It adds menu options in the right click context menu for the code editor and the same option in the Tools menu.

The following output:
```
{
  "BProp": 56,
  "DateTimeOffsetProperty": 2024-08-09T19:37:37.560+02:00,
  "DateTimeProperty": 2024-08-09T14:37:37.557,
  "DoubleProperty": 5.69,
  "EnumProp": "Three",
  "FloatProperty": 5.6,
  "InnerClass": {
    "IntKeyedIntValue": {
      "34": 4,
      "38": 6
    },
    "IntList": [
      1,
      2,
      3
    ],
    "IntProperty": -7,
    "NullableStringProperty": null,
    "StringKeyedIntValue": {
      "four": 4,
      "six": 6
    },
    "_intField": 7
  },
  "InnerClassNull": null,
  "IntArray": [
    1,
    3,
    45
  ],
  "IntKeyedObjectValue": {
    "4": {
      "IntKeyedIntValue": {
        "34": 4,
        "38": 6
      },
      "IntList": [
        1,
        2,
        3
      ],
      "IntProperty": 0,
      "NullableStringProperty": null,
      "StringKeyedIntValue": {
        "four": 4,
        "six": 6
      },
      "_intField": 7
    },
    "6": {
      "IntKeyedIntValue": {
        "34": 4,
        "38": 6
      },
      "IntList": [
        1,
        2,
        3
      ],
      "IntProperty": 0,
      "NullableStringProperty": null,
      "StringKeyedIntValue": {
        "four": 4,
        "six": 6
      },
      "_intField": 7
    },
    "7": null
  },
  "IntProperty": 8,
  "ObjectKeyedIntValue": {
    "PLGR.TestClassInner": 4,
    "PLGR.TestClassInner": 6
  },
  "StrList": [
    "one",
    "two",
    "three"
  ],
  "StrProperty": null,
  "_byteField": 56,
  "_charField": "w",
  "_enumField": "Three",
  "_flagEnumField": "One | Two",
  "_intField": 3,
  "_pubint": 9,
  "_timespanField": "-08:00:00.0000007"
}
```

Is produced by these classes:
```
internal class TestClassOuter
    {
        public List<string> StrList { get; } = new List<string>() { "one", "two", "three" };
        public int IntProperty { get; set; } = 8;

        public string? StrProperty { get; set; } = null;

        private int _intField = 3;
        public TestClassInner InnerClass { get; set; } = new TestClassInner() { IntProperty = -7 };
        public TestClassInner? InnerClassNull { get; set; }

        public RegularEnum EnumProp { get; set; } = RegularEnum.Three;

        public byte BProp { get; set; } = (byte)56;

        public RegularEnum _enumField = RegularEnum.Three;

        public byte _byteField = (byte)56;

        public int _pubint = 9;

        private char _charField = 'w';

        public double DoubleProperty { get; set; } = 5.69;

        public DateTime DateTimeProperty { get; set; } = DateTime.Now;

        public float FloatProperty { get; set; } = 5.6f;

        public int[] IntArray { get; set; } = { 1, 3, 45 };

        public DateTimeOffset DateTimeOffsetProperty { get; set; } = DateTimeOffset.Now.AddHours(5);

        public TimeSpan _timespanField = DateTime.Now - DateTime.Now.AddHours(8);

        public FlagEnum _flagEnumField = FlagEnum.Two | FlagEnum.One;

        public Dictionary<TestClassInner, int> ObjectKeyedIntValue = new Dictionary<TestClassInner, int>() { { new TestClassInner(), 4 }, { new TestClassInner() { IntProperty = 5 }, 6 } };

        public Dictionary<int, TestClassInner> IntKeyedObjectValue = new Dictionary<int, TestClassInner>() { { 4, new TestClassInner() }, { 6, new TestClassInner() }, { 7, null } };

    }

 internal class TestClassInner : IEquatable<TestClassInner>
    {
        public List<int> IntList { get; } = new List<int>() { 1, 2, 3 };
        public int IntProperty { get; set; }

        public Dictionary<string, int> StringKeyedIntValue = new Dictionary<string, int>() { { "four", 4 }, { "six", 6 } };

        public Dictionary<int, int> IntKeyedIntValue = new Dictionary<int, int>() { { 34, 4 }, { 38, 6 } };

        public string? NullableStringProperty { get; set; } = null;

        private int _intField = 7;
        public bool Equals(TestClassInner? other)
        {
            return IntProperty == other?.IntProperty;
        }
    }
```
