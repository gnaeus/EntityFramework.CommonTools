using System.Collections.Generic;
using System.Linq;
using EntityFramework.ChangeTrackingExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityFramework.Common.Tests
{
    [TestClass]
    public class JsonFieldTests
    {
        class User
        {
            private JsonField<Address> _address;
            public string AddressJson
            {
                get { return _address.Json; }
                set { _address.Json = value; }
            }
            public Address Address
            {
                get { return _address.Value; }
                set { _address.Value = value; }
            }

            private JsonField<ICollection<Phone>> _phones = new HashSet<Phone>();
            public string PhonesJson
            {
                get { return _phones.Json; }
                set { _phones.Json = value; }
            }
            public ICollection<Phone> Phones
            {
                get { return _phones.Value; }
                set { _phones.Value = value; }
            }
        }

        class Region
        {
            public string City { get; set; }
        }

        class Address : Region
        {
            public string Street { get; set; }
        }
        
        class Phone
        {
            public string Code { get; set; }
            public string Number { get; set; }
        }

        [TestMethod]
        public void ShouldUseDefaultValueWhenCreated()
        {
            var user = new User();

            Assert.IsNotNull(user.Phones);
            Assert.IsInstanceOfType(user.Phones, typeof(HashSet<Phone>));
        }

        [TestMethod]
        public void ShouldUseDefaultValueWhenDbContainsNull()
        {
            var user = new User();

            user.PhonesJson = null;

            Assert.IsNotNull(user.Phones);
            Assert.IsInstanceOfType(user.Phones, typeof(HashSet<Phone>));
        }

        [TestMethod]
        public void ShouldUseDefaultValueWhenDbContainsEmptyString()
        {
            var user = new User();

            user.PhonesJson = "";

            Assert.IsNotNull(user.Phones);
            Assert.IsInstanceOfType(user.Phones, typeof(HashSet<Phone>));
        }

        [TestMethod]
        public void ShouldUseDefaultValueWhenDbContainsNullJson()
        {
            var user = new User();

            user.PhonesJson = "null";

            Assert.IsNotNull(user.Phones);
            Assert.IsInstanceOfType(user.Phones, typeof(HashSet<Phone>));
        }

        [TestMethod]
        public void ShouldNotUseDefaultValueAfterReload()
        {
            var user = new User();

            user.PhonesJson = null;

            Assert.IsNotNull(user.Phones);

            user.PhonesJson = null;

            Assert.IsNull(user.Phones);
        }
        
        class Benchmark
        {
            private JsonField<IDictionary<string, int>> _scores = new Dictionary<string, int>();
            public string ScoresJson
            {
                get { return _scores.Json; }
                set { _scores.Json = value; }
            }
            public IDictionary<string, int> Scores
            {
                get { return _scores.Value; }
                set { _scores.Value = value; }
            }
        }

        [TestMethod]
        public void ShouldSerializeObject()
        {
            var user = new User();

            user.Address = new Address { City = "Moscow", Street = "Arbat" };

            Assert.AreEqual("{\"City\":\"Moscow\",\"Street\":\"Arbat\"}", user.AddressJson);
        }

        [TestMethod]
        public void ShouldDeserializeObject()
        {
            var user = new User();

            user.AddressJson = "{\"City\":\"Moscow\",\"Street\":\"Arbat\"}";

            var address = user.Address;

            Assert.IsNotNull(address);
            Assert.AreEqual("Moscow", address.City);
            Assert.AreEqual("Arbat", address.Street);
        }

        [TestMethod]
        public void ShouldSerializeCollection()
        {
            var user = new User();

            user.Phones = new List<Phone>
            {
                new Phone { Code = "123", Number = "456789" },
            };

            Assert.AreEqual("[{\"Number\":\"456789\",\"Code\":\"123\"}]", user.PhonesJson);
        }

        [TestMethod]
        public void ShouldDeserializeCollection()
        {
            var user = new User();

            user.PhonesJson = "[{\"Code\":\"123\",\"Number\":\"456789\"}]";

            var phones = user.Phones;

            Assert.IsNotNull(phones);
            Assert.AreEqual(1, phones.Count);
            Assert.AreEqual("123", phones.ElementAt(0).Code);
            Assert.AreEqual("456789", phones.ElementAt(0).Number);
        }

        [TestMethod]
        public void ShouldSerializeDictionary()
        {
            var benchmark = new Benchmark();

            benchmark.Scores["foo"] = 10;
            benchmark.Scores["bar"] = 20;

            Assert.AreEqual("{\"foo\":10,\"bar\":20}", benchmark.ScoresJson);
        }

        [TestMethod]
        public void ShouldDeserializeDictionary()
        {
            var benchmark = new Benchmark();

            benchmark.ScoresJson = "{\"foo\":10,\"bar\":20}";

            var scores = benchmark.Scores;

            Assert.IsNotNull(scores);
            Assert.AreEqual(2, scores.Count);
            Assert.AreEqual(10, scores["foo"]);
            Assert.AreEqual(20, scores["bar"]);
        }

        [TestMethod]
        public void ShouldUseSameObjectsWhenAccessed()
        {
            var user = new User();

            user.AddressJson = "{\"City\":\"Moscow\",\"Street\":\"Arbat\"}";

            var address1 = user.Address;

            Assert.IsNotNull(address1);
            Assert.AreSame(address1, user.Address);

            user.Address.Street = "Tverskaya";

            var address2 = user.Address;

            Assert.IsNotNull(address2);
            Assert.AreSame(address2, user.Address);

            Assert.AreSame(address1, address2);
        }

        [TestMethod]
        public void ShouldUseSameJsonWhenPropertyNotTouched()
        {
            var user = new User();

            user.AddressJson = "{\"City\":\"Moscow\",\"Street\":\"Arbat\"}";

            string addressJson = user.AddressJson;
            
            Assert.AreSame(user.AddressJson, user.AddressJson);
        }

        [TestMethod]
        public void ShouldUseEqualJsonWhenPropertyNotChanged()
        {
            var user = new User();

            user.AddressJson = "{\"City\":\"Moscow\",\"Street\":\"Arbat\"}";

            string addressJson = user.AddressJson;

            var address = user.Address;

            Assert.AreEqual(addressJson, user.AddressJson);
        }

        [TestMethod]
        public void ShouldReflectPropertyChangesToJson()
        {
            var user = new User();

            user.AddressJson = "{\"City\":\"Moscow\",\"Street\":\"Arbat\"}";

            user.Address.Street = "Tverskaya";

            string addressJson = user.AddressJson;

            Assert.AreEqual("{\"City\":\"Moscow\",\"Street\":\"Tverskaya\"}", user.AddressJson);
        }

        [TestMethod]
        public void ShouldSerializeNullToDbNull()
        {
            var user = new User();

            user.PhonesJson = "[]";

            user.Phones = null;

            Assert.IsNull(user.PhonesJson);
        }

        [TestMethod]
        public void ShouldExtendIncompleteJson()
        {
            var user = new User();

            user.AddressJson = "{\"Street\":\"Arbat\"}";
            user.PhonesJson = "[{\"Number\":\"456789\"},{}]";

            var address = user.Address;
            var phones = user.Phones;

            Assert.AreEqual("{\"City\":null,\"Street\":\"Arbat\"}", user.AddressJson);
            Assert.AreEqual("[{\"Number\":\"456789\",\"Code\":null},{\"Number\":null,\"Code\":null}]", user.PhonesJson);
        }

        class Settings
        {
            public string Key { get; set; }

            JsonField<dynamic> _value;
            public string ValueJson
            {
                get { return _value.Json; }
                set { _value.Json = value; }
            }
            public dynamic Value
            {
                get { return _value.Value; }
                set { _value.Value = value; }
            }
        }

        [TestMethod]
        public void ShouldWorkWithDynamic()
        {
            var settings = new Settings
            {
                ValueJson = "{\"Number\": 123, \"String\": \"test\"}",
            };

            Assert.IsNotNull(settings.Value);
            Assert.AreEqual(123, (int)settings.Value.Number);
            Assert.AreEqual("test", (string)settings.Value.String);
        }
    }
}
