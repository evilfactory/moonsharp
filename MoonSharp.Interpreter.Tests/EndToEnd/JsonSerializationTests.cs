﻿using System.Globalization;
using System.Threading;
using MoonSharp.Interpreter.Serialization.Json;
using NUnit.Framework;


namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class JsonSerializationTests
	{
		void AssertTableValues(Table t)
		{
			Assert.AreEqual(DataType.Number, t.Get("aNumber").Type);
			Assert.AreEqual(1, t.Get("aNumber").Number);

			Assert.AreEqual(DataType.String, t.Get("aString").Type);
			Assert.AreEqual("2", t.Get("aString").String);

			Assert.AreEqual(DataType.Table, t.Get("anObject").Type);
			Assert.AreEqual(DataType.Table, t.Get("anArray").Type);

			Assert.AreEqual(DataType.String, t.Get("slash").Type);
			Assert.AreEqual("a/b", t.Get("slash").String);

			Table o = t.Get("anObject").Table;

			Assert.AreEqual(DataType.Number, o.Get("aNumber").Type);
			Assert.AreEqual(3, o.Get("aNumber").Number);

			Assert.AreEqual(DataType.String, o.Get("aString").Type);
			Assert.AreEqual("4", o.Get("aString").String);

			Table a = t.Get("anArray").Table;

			//				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ]

			Assert.AreEqual(DataType.Number, a.Get(1).Type);
			Assert.AreEqual(5, a.Get(1).Number);

			Assert.AreEqual(DataType.String, a.Get(2).Type);
			Assert.AreEqual("6", a.Get(2).String);

			Assert.AreEqual(DataType.Boolean, a.Get(3).Type);
			Assert.IsTrue(a.Get(3).Boolean);

			Assert.AreEqual(DataType.Boolean, a.Get(3).Type);
			Assert.IsTrue(a.Get(3).Boolean);

			Assert.AreEqual(DataType.UserData, a.Get(4).Type);
			Assert.IsTrue(JsonNull.IsJsonNull(a.Get(4)));

			Assert.AreEqual(DataType.Table, a.Get(5).Type);
			Table s = a.Get(5).Table;

			Assert.AreEqual(DataType.Number, s.Get("aNumber").Type);
			Assert.AreEqual(7, s.Get("aNumber").Number);

			Assert.AreEqual(DataType.String, s.Get("aString").Type);
			Assert.AreEqual("8", s.Get("aString").String);

			Assert.AreEqual(DataType.Number, t.Get("aNegativeNumber").Type);
			Assert.AreEqual(-9, t.Get("aNegativeNumber").Number);
		}


		[Test]
		public void JsonDeserialization()
		{
			string json = @"{
				'aNumber' : 1,
				'aString' : '2',
				'anObject' : { 'aNumber' : 3, 'aString' : '4' },
				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
				'aNegativeNumber' : -9,
				'slash' : 'a\/b'
				}
			".Replace('\'', '\"');

			Table t = JsonTableConverter.JsonToTable(json);
			AssertTableValues(t);
		}

		[Test]
		public void JsonSerialization()
		{
			string json = @"{
				'aNumber' : 1,
				'aString' : '2',
				'anObject' : { 'aNumber' : 3, 'aString' : '4' },
				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
				'aNegativeNumber' : -9,
				'slash' : 'a\/b'
				}
			".Replace('\'', '\"');

			Table t1 = JsonTableConverter.JsonToTable(json);

			string json2 = JsonTableConverter.TableToJson(t1);

			Table t = JsonTableConverter.JsonToTable(json2);

			AssertTableValues(t);
		}


		[Test]
		public void JsonObjectSerialization()
		{
			object o = new
			{
				aNumber = 1,
				aString = "2",
				anObject = new
				{
					aNumber = 3,
					aString = "4"
				},
				anArray = new object[]
				{
					5,
					"6",
					true,
					null,
					new
					{
						aNumber = 7,
						aString = "8"
					}
				},
				aNegativeNumber = -9,
				slash = "a/b"
			};


			string json = JsonTableConverter.ObjectToJson(o);

			Table t = JsonTableConverter.JsonToTable(json);

			AssertTableValues(t);
		}

		[Test]
		public void SerializeNoLocalize()
		{
			// German uses , as a decimal separator, we don't want that
			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
			Assert.AreEqual("{\"test\":0.01}", Script.RunString("return json.serialize({test = 0.01})").String);
		}

		[Test]
		public void ParseNoLocalize()
		{
			// German uses , as a decimal separator, we don't want that
			Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
			Assert.AreEqual(1.23, Script.RunString("return json.parse('{\"test\":1.23}').test").Number);
		}

	}
}
