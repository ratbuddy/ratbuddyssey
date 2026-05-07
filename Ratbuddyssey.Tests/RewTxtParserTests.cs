#nullable disable
#pragma warning disable CA1305, CA1861
using System.Collections.Generic;
using System.IO;
using Ratbuddyssey.Features.REW;
using Xunit;

namespace Ratbuddyssey.Tests
{
    public class RewTxtParserTests
    {
        private static readonly string[] ValidBody =
        {
            "* Measurement: My Sub",
            "* Source: REW V5.20",
            "* Freq(Hz)  SPL(dB)  Phase(deg)",
            "20.0  72.3  -10.2",
            "25.0  74.1  -11.0",
            "31.5  76.8  -12.1",
            "40.0  78.0  -13.0",
            "50.0  79.5  -14.0",
            "63.0  80.1  -15.0",
            "80.0  79.0  -16.0",
            "100.0 77.5  -17.0",
            "125.0 75.0  -18.0",
            "",
        };

        [Fact]
        public void Parses_Standard_Tab_Space_Header()
        {
            var r = new RewTxtParser().Parse(ValidBody, "Sub");
            Assert.True(r.Success);
            Assert.Equal(9, r.Measurement.Points.Count);
            Assert.Equal("Sub", r.Measurement.Name);
            Assert.Equal(20.0, r.Measurement.Points[0].FrequencyHz);
            Assert.Equal(72.3, r.Measurement.Points[0].SplDb, 3);
        }

        [Fact]
        public void Comma_Decimal_Is_Accepted_When_No_Dot_Present()
        {
            var lines = new[]
            {
                "20,0\t72,3",
                "25,0\t74,1",
                "31,5\t76,8",
                "40,0\t78,0",
                "50,0\t79,5",
                "63,0\t80,1",
                "80,0\t79,0",
                "100,0\t77,5",
            };
            var r = new RewTxtParser().Parse(lines);
            Assert.True(r.Success);
            Assert.Equal(8, r.Measurement.Points.Count);
            Assert.Equal(72.3, r.Measurement.Points[0].SplDb, 3);
        }

        [Fact]
        public void Header_And_Comment_Lines_Are_Skipped()
        {
            var r = new RewTxtParser().Parse(ValidBody);
            Assert.True(r.Success);
            // 3 header lines + 1 blank → 4 skipped
            Assert.Equal(4, r.LinesSkipped);
        }

        [Fact]
        public void Too_Few_Points_Fails_Gracefully()
        {
            var lines = new[] { "20 72", "25 74" };
            var r = new RewTxtParser().Parse(lines);
            Assert.False(r.Success);
            Assert.NotNull(r.Error);
            Assert.Contains("at least", r.Error);
        }

        [Fact]
        public void Garbage_Rows_Are_Skipped_Not_Crashing()
        {
            var lines = new List<string>
            {
                "this is not a row",
                "* header",
                "1e9 99",         // hz out of bounds
                "20 -300",        // dB out of bounds
                "abc def",
            };
            lines.AddRange(ValidBody);
            var r = new RewTxtParser().Parse(lines);
            Assert.True(r.Success);
            Assert.Equal(9, r.Measurement.Points.Count);
        }

        [Fact]
        public void Missing_File_Returns_Error_Not_Throw()
        {
            var r = new RewTxtParser().ParseFile(@"C:\definitely\does\not\exist.txt");
            Assert.False(r.Success);
            Assert.NotNull(r.Error);
        }

        [Fact]
        public void Null_Or_Empty_Path_Returns_Error()
        {
            var p = new RewTxtParser();
            Assert.False(p.ParseFile(null).Success);
            Assert.False(p.ParseFile("").Success);
            Assert.False(p.ParseFile("   ").Success);
        }

        [Fact]
        public void Round_Trip_Through_Disk()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllLines(path, ValidBody);
                var r = new RewTxtParser().ParseFile(path);
                Assert.True(r.Success);
                Assert.Equal(9, r.Measurement.Points.Count);
                Assert.Equal(Path.GetFileNameWithoutExtension(path), r.Measurement.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
