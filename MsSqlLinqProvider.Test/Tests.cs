using System.Linq.Expressions;
namespace MsSqlLinqProvider.Test
{
    public class Tests
    {
        [Fact]
        public void TranslatorTest()
        {
            var translator = new Translator();
            Expression<Func<IQueryable<Product>, IQueryable<Product>>> expression
                = products => products.Where(p => p.UnitPrice > 100 && p.ProductType == "Customised Product");
            string translated = translator.Translate(expression);
            var expected = "SELECT * FROM products WHERE UnitPrice>100 AND ProductType='Customised Product'";
            Assert.Equal(expected, translated);
        }
    }
}