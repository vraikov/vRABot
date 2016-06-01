using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace vRABot.vRA
{
    public class APIClientException : Exception
    {
        public APIClientException()
            : base() { }

        public APIClientException(string message)
            : base(message) { }

        public APIClientException(string message, Exception inner)
            : base(message, inner) { }
    }
}