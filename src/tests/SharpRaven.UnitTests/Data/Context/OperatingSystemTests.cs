﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OperatingSystem = SharpRaven.Data.Context.OperatingSystem;

namespace SharpRaven.UnitTests.Data.Context
{
    [TestFixture]
    public class OperatingSystemTests
    {
        [Test]
        public void Create_RawDescription_SameAsEnvironment()
        {
            var operatingSystem = OperatingSystem.Create();

#if HAS_RUNTIME_INFORMATION
            // Microsoft Windows 10.0.16299
            var expected = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#else
            var expected = Environment.OSVersion.VersionString;
#endif

            Assert.NotNull(operatingSystem.RawDescription);
            Assert.AreEqual(expected, operatingSystem.RawDescription);
        }

        [Test]
        public void Ctor_NoPropertyFilled_SerializesEmptyObject()
        {
            var operatingSystem = new OperatingSystem();

            var actual = JsonConvert.SerializeObject(operatingSystem);

            Assert.That(actual, Is.EqualTo("{}"));
        }

        [Test]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var operatingSystem = new OperatingSystem
            {
                Name = "Windows",
                KernelVersion = "who knows",
                Version = "2016",
                RawDescription = "Windows 2016",
                Build = "14393",
                Rooted = true
            };

            var actual = JsonConvert.SerializeObject(operatingSystem);

            Assert.That(actual, Is.EqualTo(
                         "{\"name\":\"Windows\","
                        + "\"version\":\"2016\","
                        + "\"raw_description\":\"Windows 2016\","
                        + "\"build\":\"14393\","
                        + "\"kernel_version\":\"who knows\","
                        + "\"rooted\":true}"));
        }

        [Test, TestCaseSource(typeof(OperatingSystemTests), nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected(TestCase @case)
        {
            var actual = JsonConvert.SerializeObject(@case.Object);

            Assert.That(actual, Is.EqualTo(@case.ExpectedSerializationOutput));
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { new TestCase
            {
                Object = new OperatingSystem(),
                ExpectedSerializationOutput = "{}"
            }};

            yield return new object[] { new TestCase
                {
                    Object = new OperatingSystem { Name = "some name" },
                    ExpectedSerializationOutput = "{\"name\":\"some name\"}"
                }};

            yield return new object[] { new TestCase
            {
                Object = new OperatingSystem { RawDescription = "some Name, some version" },
                ExpectedSerializationOutput = "{\"raw_description\":\"some Name, some version\"}"
            }};

            yield return new object[] { new TestCase
            {
                Object = new OperatingSystem { Build = "some build" },
                ExpectedSerializationOutput = "{\"build\":\"some build\"}"
            }};

            yield return new object[] { new TestCase
            {
                Object = new OperatingSystem { KernelVersion = "some kernel version" },
                ExpectedSerializationOutput = "{\"kernel_version\":\"some kernel version\"}"
            }};

            yield return new object[] { new TestCase
            {
                Object = new OperatingSystem { Rooted = false },
                ExpectedSerializationOutput = "{\"rooted\":false}"
            }};
        }
    }
}
