﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace UnitTest.Rollbar.Serialization.Json
{
    using global::Rollbar;
    using global::Rollbar.Serialization.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Threading;

    [TestClass]
    [TestCategory(nameof(JsonScrubberFixture))]
    public class JsonScrubberFixture
    {
        [TestInitialize]
        public void SetupFixture()
        {
        }

        [TestCleanup]
        public void TearDownFixture()
        {
        }

        [TestMethod]
        public void BasicScrubByNameTest()
        {
            var jsonString = @"{'results' : [
              {
                 'address_components' : 'abc' ,
                 'formatted_address' : 'eedfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : 56.88225340,
                          'lng' : 7.34169940
                       },
                       'southwest' : {
                          'lat' : 2.4792219750,
                          'lng' : 6.85382840
                       }}}},
              {
                 'address_components' : 'abc1' ,
                 'formatted_address' : 'ffdfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : 6.88225340,
                          'lng' : 17.34169940
                       },
                       'southwest' : {
                          'lat' : 22.4792219750,
                          'lng' : 16.85382840
                       }}}}

               ]}";

            string[] scrubFields = new string[] {
                //"lng",
                "lat",
                "southwest",
            };

            string scrubbedJsonString = JsonScrubber.ScrubJsonFieldsByName(jsonString, scrubFields, "***");

            var expectedResult = @"{'results' : [
              {
                 'address_components' : 'abc' ,
                 'formatted_address' : 'eedfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : '***',
                          'lng' : 7.34169940
                       },
                       'southwest' : '***'
              }}},
              {
                 'address_components' : 'abc1' ,
                 'formatted_address' : 'ffdfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : '***',
                          'lng' : 17.34169940
                       },
                       'southwest' : '***'
               }}}
               ]}";

            Assert.AreEqual(JObject.Parse(expectedResult).ToString(), JObject.Parse(scrubbedJsonString).ToString());

            scrubFields = new string[] {
                "lng",
                "lat",
                "southwest",
            };

            scrubbedJsonString = JsonScrubber.ScrubJsonFieldsByName(jsonString, scrubFields, "***");

            Assert.AreNotEqual(JObject.Parse(expectedResult).ToString(), JObject.Parse(scrubbedJsonString).ToString());
        }

        [TestMethod]
        public void BasicScrubByPathTest()
        {
            var jsonString = @"{'results' : [
              {
                 'address_components' : 'abc' ,
                 'formatted_address' : 'eedfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : 56.88225340,
                          'lng' : 7.34169940
                       },
                       'southwest' : {
                          'lat' : 2.4792219750,
                          'lng' : 6.85382840
                       }}}},
              {
                 'address_components' : 'abc1' ,
                 'formatted_address' : 'ffdfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : 6.88225340,
                          'lng' : 17.34169940
                       },
                       'southwest' : {
                          'lat' : 22.4792219750,
                          'lng' : 16.85382840
                       }}}}

               ]}";

            string[] scrubFields = new string[] {
                //"lng",
                "results[0].geometry.bounds.northeast.lat",
                "results[0].geometry.bounds.southwest",
            };

            string scrubbedJsonString = JsonScrubber.ScrubJsonFieldsByPaths(jsonString, scrubFields, "***");

            var expectedResult = @"{'results' : [
              {
                 'address_components' : 'abc' ,
                 'formatted_address' : 'eedfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : '***',
                          'lng' : 7.34169940
                       },
                       'southwest' : '***'
              }}},
              {
                 'address_components' : 'abc1' ,
                 'formatted_address' : 'ffdfdfdfdfdfdfdf',
                 'geometry' : {
                    'bounds' : {
                       'northeast' : {
                          'lat' : 6.88225340,
                          'lng' : 17.34169940
                       },
                       'southwest' : {
                          'lat' : 22.4792219750,
                          'lng' : 16.85382840
                       }}}}

               ]}";

            Assert.AreEqual(JObject.Parse(expectedResult).ToString(), JObject.Parse(scrubbedJsonString).ToString());

            scrubFields = new string[] {
                "lng",
                "lat",
                "southwest",
            };

            expectedResult = @"
            {
                'results': [
                {
                    'address_components': 'abc',
                    'formatted_address': 'eedfdfdfdfdfdfdf',
                    'geometry': {
                        'bounds': {
                            'northeast': {
                                'lat': '***',
                                'lng': '***'
                            },
                            'southwest': '***'
                        }
                    }
                },
                {
                    'address_components': 'abc1',
                    'formatted_address': 'ffdfdfdfdfdfdfdf',
                    'geometry': {
                        'bounds': {
                            'northeast': {
                                'lat': '***',
                                'lng': '***'
                            },
                            'southwest': '***'
                        }
                    }
                }
                ]
            }";

            scrubbedJsonString = JsonScrubber.ScrubJsonFieldsByName(jsonString, scrubFields, "***");

            Assert.AreEqual(JObject.Parse(expectedResult).ToString(), JObject.Parse(scrubbedJsonString).ToString());
        }

        [TestMethod]
        public void CustomDottedDataScrubByPathTest()
        {
            var jsonString = @"{'results' : [
            {
                'address_components' : 'abc' ,
                'formatted_address' : 'eedfdfdfdfdfdfdf',
                'geometry' : {
                'bounds' : {
                    'northeast' : {
                        'lat.x' : 56.88225340,
                        'lng.y' : 7.34169940
                    },
                    'southwest' : {
                        'lat.x' : 2.4792219750,
                        'lng.y' : 6.85382840
                    }}}},
            {
                'address_components' : 'abc1' ,
                'formatted_address' : 'ffdfdfdfdfdfdfdf',
                'geometry' : {
                'bounds' : {
                    'northeast' : {
                        'lat.x' : 6.88225340,
                        'lng.y' : 17.34169940
                    },
                    'southwest' : {
                        'lat.x' : 22.4792219750,
                        'lng.y' : 16.85382840
                    }}}}

            ]}";

            string[] scrubFields = new string[] {
                //"lng",
                "results[0].geometry.bounds.northeast.lat.x",
                "results[0].geometry.bounds.northeast.lng.y",
                "results[1].geometry.bounds.northeast.lng.y",
                "results[1].geometry.bounds.southwest.lat.x",
            };

            var expectedResult = @"{'results' : [
            {
                'address_components' : 'abc' ,
                'formatted_address' : 'eedfdfdfdfdfdfdf',
                'geometry' : {
                'bounds' : {
                    'northeast' : {
                        'lat.x' : '***',
                        'lng.y' : '***'
                    },
                    'southwest' : {
                        'lat.x' : 2.4792219750,
                        'lng.y' : 6.85382840
                    }}}},
            {
                'address_components' : 'abc1' ,
                'formatted_address' : 'ffdfdfdfdfdfdfdf',
                'geometry' : {
                'bounds' : {
                    'northeast' : {
                        'lat.x' : 6.88225340,
                        'lng.y' : '***'
                    },
                    'southwest' : {
                        'lat.x' : '***',
                        'lng.y' : 16.85382840
                    }}}}

            ]}";

            var scrubbedJsonString = JsonScrubber.ScrubJsonFieldsByPaths(jsonString, scrubFields, "***");

            Assert.AreEqual(JObject.Parse(expectedResult).ToString(), JObject.Parse(scrubbedJsonString).ToString());

        }
    }
}
