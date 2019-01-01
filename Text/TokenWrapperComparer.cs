using System.Collections.Generic;

namespace HC.Core.Text
{
    public class TokenWrapperComparer : IEqualityComparer<TokenWrapper>
    {
        public bool Equals(TokenWrapper x, TokenWrapper y)
        {
            return x.Token.Equals(y.Token);
        }

        public int GetHashCode(TokenWrapper obj)
        {
            return obj.HashCode;
        }
    }
}
