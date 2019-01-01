#region

using System;
using System.Collections;
using System.Collections.Generic;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Text
{
    public class TokenWrapper :
        IEquatable<TokenWrapper>,
        IComparable<TokenWrapper>,
        IComparer<TokenWrapper>,
        IComparer,
        IEqualityComparer<TokenWrapper>
    {
        #region Properties

        public int Length
        {
            get { return Token.Length; }
        }

        public string Token { get; private set; }
        public int HashCode { get; private set; }

        #endregion

        #region Constructors

        public TokenWrapper()
        {
        }

        public TokenWrapper(string strToken)
        {
            if (string.IsNullOrEmpty(strToken))
            {
                throw new HCException("Invalid token");
            }
            SetToken(strToken);
        }

        public char this[int i]
        {
            get { return Token[i]; }
        }

        public void SetToken(string strToken)
        {
            Token = string.Intern(strToken);
            HashCode = strToken.GetHashCode();
        }

        #endregion

        #region Public

        #region IComparable<TokenWrapper> Members

        public int CompareTo(TokenWrapper other)
        {
            return Compare(this, other);
        }

        #endregion

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare(
                (TokenWrapper) x,
                (TokenWrapper) y);
        }

        #endregion

        #region IComparer<TokenWrapper> Members

        public int Compare(TokenWrapper x, TokenWrapper y)
        {
            return x.Token.CompareTo(y.Token);
        }

        #endregion

        #region IEqualityComparer<TokenWrapper> Members

        public bool Equals(TokenWrapper x, TokenWrapper y)
        {
            //if (x.HashCode != y.HashCode)
            //{
            //    return false;
            //}
            return x.Token.Equals(y.Token);
        }

        public int GetHashCode(TokenWrapper obj)
        {
            return obj.HashCode;
        }

        #endregion

        #region IEquatable<TokenWrapper> Members

        public bool Equals(TokenWrapper other)
        {
            return Equals(this, other);
        }

        #endregion

        public override string ToString()
        {
            return Token;
        }

        #endregion
    }
}


