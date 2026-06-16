using System.Linq.Expressions;
using Core;

namespace Core.Tests
{
    public class DocumentKeyTests
    {
        private sealed class Sample
        {
            public string Name { get; set; } = string.Empty;

            public int Number { get; set; }
        }

        [Fact]
        public void SelectorFromExpression_StringMember_ReturnsValue()
        {
            Func<Sample, string> selector = DocumentKey.SelectorFromExpression<Sample>(s => s.Name);

            Assert.Equal("abc", selector(new Sample { Name = "abc" }));
        }

        [Fact]
        public void SelectorFromExpression_ValueTypeMember_UsesInvariantString()
        {
            Func<Sample, string> selector = DocumentKey.SelectorFromExpression<Sample>(s => s.Number);

            Assert.Equal("42", selector(new Sample { Number = 42 }));
        }

        [Fact]
        public void MemberName_StringMember_ReturnsName()
        {
            Assert.Equal("Name", DocumentKey.MemberName<Sample>(s => s.Name));
        }

        [Fact]
        public void MemberName_ValueTypeMember_UnwrapsConvert()
        {
            Assert.Equal("Number", DocumentKey.MemberName<Sample>(s => s.Number));
        }

        [Fact]
        public void MemberName_NonMemberExpression_Throws()
        {
            Expression<Func<Sample, object>> expr = s => s.Name + s.Number;

            Assert.Throws<ArgumentException>(() => DocumentKey.MemberName(expr));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmptyKey_Throws(string? key)
        {
            Assert.Throws<ArgumentException>(() => DocumentKey.Validate(key));
        }

        [Fact]
        public void Validate_ValidKey_ReturnsKey()
        {
            Assert.Equal("ok", DocumentKey.Validate("ok"));
        }
    }
}
