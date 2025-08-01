// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.CmdPal.Ext.TimeDate.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CmdPal.Ext.TimeDate.UnitTests;

[TestClass]
public class StringParserTests
{
    private CultureInfo originalCulture;
    private CultureInfo originalUiCulture;

    [TestInitialize]
    public void Setup()
    {
        // Set culture to 'en-us'
        originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-us", false);
        originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-us", false);
    }

    [DataTestMethod]
    [DataRow("10/29/2022 17:05:10", true, "G", "10/29/2022 5:05:10 PM")]
    [DataRow("Saturday, October 29, 2022 5:05:10 PM", true, "G", "10/29/2022 5:05:10 PM")]
    [DataRow("10/29/2022", true, "d", "10/29/2022")]
    [DataRow("Saturday, October 29, 2022", true, "d", "10/29/2022")]
    [DataRow("17:05:10", true, "T", "5:05:10 PM")]
    [DataRow("5:05:10 PM", true, "T", "5:05:10 PM")]
    [DataRow("10456", false, "", "")]
    [DataRow("u10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("u-10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("u+10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("ums10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("ums-10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("ums+10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("ft10456", true, "", "")] // Value is UTC and can be different based on system
    [DataRow("oa-657434.99999999", true, "G", "1/1/0100 11:59:59 PM")]
    [DataRow("oa2958465.99999999", true, "G", "12/31/9999 11:59:59 PM")]
    [DataRow("oa-657435", false, "", "")] // Value to low
    [DataRow("oa2958466", false, "", "")] // Value to large
    [DataRow("exc1.99998843", true, "G", "1/1/1900 11:59:59 PM")]
    [DataRow("exc59.99998843", true, "G", "2/28/1900 11:59:59 PM")]
    [DataRow("exc61", true, "G", "3/1/1900 12:00:00 AM")]
    [DataRow("exc62.99998843", true, "G", "3/2/1900 11:59:59 PM")]
    [DataRow("exc2958465.99998843", true, "G", "12/31/9999 11:59:59 PM")]
    [DataRow("exc0", false, "", "")] // Day 0 means in Excel 0/1/1900 and this is a fake date.
    [DataRow("exc0.99998843", false, "", "")] // Day 0 means in Excel 0/1/1900 and this is a fake date.
    [DataRow("exc60.99998843", false, "", "")] // Day 60 means in Excel 2/29/1900 and this is a fake date in Excel which we cannot support.
    [DataRow("exc60", false, "", "")] // Day 60 means in Excel 2/29/1900 and this is a fake date in Excel which we cannot support.
    [DataRow("exc-1", false, "", "")] // Value to low
    [DataRow("exc2958466", false, "", "")] // Value to large
    [DataRow("exf0.99998843", true, "G", "1/1/1904 11:59:59 PM")]
    [DataRow("exf2957003.99998843", true, "G", "12/31/9999 11:59:59 PM")]
    [DataRow("exf-0.5", false, "", "")] // Value to low
    [DataRow("exf2957004", false, "", "")] // Value to large
    public void ConvertStringToDateTime(string typedString, bool expectedBool, string stringType, string expectedString)
    {
        // Act
        var boolResult = TimeAndDateHelper.ParseStringAsDateTime(in typedString, out DateTime result, out _);

        // Assert
        Assert.AreEqual(expectedBool, boolResult);
        if (!string.IsNullOrEmpty(expectedString))
        {
            Assert.AreEqual(expectedString, result.ToString(stringType, CultureInfo.CurrentCulture));
        }
    }

    [TestMethod]
    public void ParseStringAsDateTime_BasicTest()
    {
        // Test basic string parsing functionality
        var testCases = new[]
        {
            ("2023-12-25", true),
            ("12/25/2023", true),
            ("invalid date", false),
            (string.Empty, false),
        };

        foreach (var (input, expectedSuccess) in testCases)
        {
            // Act
            var result = TimeAndDateHelper.ParseStringAsDateTime(in input, out DateTime dateTime, out var errorMessage);

            // Assert
            Assert.AreEqual(expectedSuccess, result, $"Failed for input: {input}");
            if (!expectedSuccess)
            {
                Assert.IsFalse(string.IsNullOrEmpty(errorMessage), $"Error message should not be empty for invalid input: {input}");
            }
        }
    }

    [TestMethod]
    public void ParseStringAsDateTime_UnixTimestampTest()
    {
        // Test Unix timestamp parsing
        var unixTimestamp = "u1640995200"; // 2022-01-01 00:00:00 UTC

        // Act
        var result = TimeAndDateHelper.ParseStringAsDateTime(in unixTimestamp, out DateTime dateTime, out var errorMessage);

        // Assert
        Assert.IsTrue(result, "Unix timestamp parsing should succeed");
        Assert.IsTrue(string.IsNullOrEmpty(errorMessage), "Error message should be empty for valid Unix timestamp");
    }

    [TestMethod]
    public void ParseStringAsDateTime_FileTimeTest()
    {
        // Test Windows file time parsing
        var fileTime = "ft132857664000000000"; // Some valid file time

        // Act
        var result = TimeAndDateHelper.ParseStringAsDateTime(in fileTime, out DateTime dateTime, out var errorMessage);

        // Assert
        Assert.IsTrue(result, "File time parsing should succeed");
    }

    [TestCleanup]
    public void CleanUp()
    {
        // Set culture to original value
        CultureInfo.CurrentCulture = originalCulture;
        CultureInfo.CurrentUICulture = originalUiCulture;
    }
}
